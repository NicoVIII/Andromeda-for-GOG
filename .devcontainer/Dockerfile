# [Choice] .NET Core version: 5.0
ARG VARIANT="5.0"
FROM mcr.microsoft.com/vscode/devcontainers/dotnet:${VARIANT}

# [Optional] Uncomment this section to install additional OS packages.
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install xz-utils zip nano \
    #
    # Provide dependencies for gui application
    && apt-get -y install xorg
