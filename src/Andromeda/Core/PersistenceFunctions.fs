namespace Andromeda.Core.FSharp

open Couchbase.Lite
open Microsoft.FSharp.Reflection
open System

module Helpers =
    // Taken from: http://www.fssnip.net/1L/title/Convert-a-obj-list-to-a-typed-list-without-generics
    type ReflectiveListBuilder =
        static member BuildList<'a> (args: obj list) =
            [ for a in args do yield a :?> 'a ]
        static member BuildTypedList lType (args: obj list) =
            typeof<ReflectiveListBuilder>
                .GetMethod("BuildList")
                .MakeGenericMethod([|lType|])
                .Invoke(null, [|args|])

    type CachingReflectiveListBuilder =
        static member ReturnTypedListBuilder<'a> () : obj list -> obj =
            let createList (args : obj list) = [ for a in args do yield a :?> 'a ] :> obj
            createList
        static member private builderMap = ref Map.empty<string, obj list -> obj>
        static member BuildTypedList (lType: System.Type) =
            let currentMap = !CachingReflectiveListBuilder.builderMap
            if Map.containsKey (lType.FullName) currentMap then
                currentMap.[lType.FullName]
            else
               let builder = typeof<CachingReflectiveListBuilder>
                                .GetMethod("ReturnTypedListBuilder")
                                .MakeGenericMethod([|lType|])
                                .Invoke(null, null)
                                :?> obj list -> obj
               CachingReflectiveListBuilder.builderMap := Map.add lType.FullName builder currentMap
               builder

    let isOption (t:Type) =
        t.IsGenericType &&
        t.GetGenericTypeDefinition() = typedefof<Option<_>>

    let isList (t:Type) =
        t.IsGenericType &&
        t.GetGenericTypeDefinition() = typedefof<List<_>>

    let convertFromArrayObject fnc array  =
        let rec helper lst index fnc (array: ArrayObject)  =
            if array.Count > index then
                let out = (fnc index array)::lst
                helper out (index+1) fnc array
            else
                lst
        helper [] 0 fnc array

    let makeOptionValue typey v isSome =
        let createOptionType typeParam =
            typeof<unit option>.GetGenericTypeDefinition().MakeGenericType([| typeParam |])

        let optionType = createOptionType typey
        let cases = FSharp.Reflection.FSharpType.GetUnionCases(optionType)
        let cases = cases |> Array.partition (fun x -> x.Name = "Some")
        let someCase = fst cases |> Array.exactlyOne
        let noneCase = snd cases |> Array.exactlyOne
        let relevantCase, args =
            match isSome with
            | true -> someCase, [| v |]
            | false -> noneCase, [| |]
        FSharp.Reflection.FSharpValue.MakeUnion(relevantCase, args)

// TODO: Return result types (see also PersistenceTypes)
module PersistenceFunctions =
    let rec loadDictionary (typeObj: Type) (dictionary: IDictionaryObject) =
        let rec loadType (typeObj: Type) (name: string) =
            match typeObj with
            | typeObj when
                typeObj = typeof<string>
                || typeObj = typeof<double>
                || typeObj = typeof<single>
                || typeObj = typeof<int>
                || typeObj = typeof<int64>
                || typeObj = typeof<bool> ->
                dictionary.GetValue(name)
            | typeObj when Helpers.isList typeObj ->
                let subType = typeObj.GetGenericArguments().[0]
                dictionary.GetArray(name)
                |> Helpers.convertFromArrayObject (fun index array ->
                    match subType with
                    | subType when
                        subType = typeof<string>
                        || subType = typeof<double>
                        || subType = typeof<single>
                        || subType = typeof<int>
                        || subType = typeof<int64>
                        || subType = typeof<bool> ->
                        array.GetValue index
                    | subType when FSharpType.IsRecord subType ->
                        array.GetDictionary index
                        |> loadDictionary subType
                )
                |> Helpers.CachingReflectiveListBuilder.BuildTypedList subType
            | typeObj when Helpers.isOption typeObj ->
                let subType = typeObj.GetGenericArguments().[0]
                let value = loadType subType name
                match value with
                | null ->
                    Helpers.makeOptionValue subType () false
                | value ->
                    Helpers.makeOptionValue subType value true
            | typeObj when FSharpType.IsRecord typeObj ->
                // TODO: optimize recursion away (it's no tail recursion)
                dictionary.GetDictionary(name)
                |> loadDictionary typeObj
            | typeObj ->
                failwithf "Given type is not supported: %s" typeObj.FullName

        let values =
            typeObj
            |> FSharpType.GetRecordFields
            |> List.ofArray
            |> List.map (fun propertyInfo ->
                let name = propertyInfo.Name
                let propertyType = propertyInfo.PropertyType
                loadType propertyType name
            )
            |> List.toArray
        FSharpValue.MakeRecord(typeObj, values)

    let loadDocument<'T> (database: Database) (documentName: string) =
        use doc = database.GetDocument(documentName)

        match typeof<'T> with
        | typeObj when FSharpType.IsRecord typeObj ->
            (loadDictionary typeObj doc) :?> 'T
        | typeObj ->
            failwithf "Given type is no record: %s" typeObj.FullName

    let loadDocumentWithMapping<'TPersistence, 'T> (mapping: 'TPersistence -> 'T) (database: Database) (documentName: string) =
        loadDocument<'TPersistence> database documentName
        |> mapping

    let rec saveDictionary (dictionary: IMutableDictionary) (value: obj) =
        let rec saveType (name: string) (value: obj) =
            match value with
            | :? string
            | :? double
            | :? single
            | :? int
            | :? int64
            | :? bool ->
                dictionary.SetValue(name, value) |> ignore
            | :? List<_> ->
                let arrayObj = MutableArrayObject()
                value :?> List<_>
                |> List.iter (fun v ->
                    match box v with
                    | :? string
                    | :? double
                    | :? single
                    | :? int
                    | :? int64
                    | :? bool ->
                        arrayObj.AddValue(v) |> ignore
                    // TODO: Add list
                    | v ->
                        v.GetType().FullName
                        |> failwithf "Given type is not supported: %s"
                )
                dictionary.SetArray (name, arrayObj) |> ignore
            | :? Option<_> ->
                // TODO:
                ()
            | value when value.GetType() |> FSharpType.IsRecord  ->
                // TODO:
                ()
            | value ->
                value.GetType().FullName
                |> failwithf "Given type is not supported: %s"

        value.GetType()
        |> FSharpType.GetRecordFields
        |> List.ofArray
        |> List.iter (fun propertyInfo ->
            let name = propertyInfo.Name
            saveType name value
        )
        ()

    let saveDocument<'T> (database: Database) (documentName: string) (record: 'T) =
        use doc =
            match database.GetDocument documentName with
            | null -> new MutableDocument(documentName)
            | x -> x.ToMutable ()

        match typeof<'T> with
        | typeObj when FSharpType.IsRecord typeObj ->
            saveDictionary doc record
        | typeObj ->
            failwithf "Given type is no record: %s" typeObj.FullName

    let saveDocumentWithMapping<'TPersistence, 'T> (mapping: 'T -> 'TPersistence) (database: Database) (documentName: string) (record: 'T) =
        mapping record
        |> saveDocument database documentName
