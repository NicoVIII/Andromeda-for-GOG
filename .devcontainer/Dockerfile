# [Choice] .NET Core version: 3.1, 2.1
ARG VARIANT=3.1
FROM mcr.microsoft.com/vscode/devcontainers/dotnetcore:dev-${VARIANT}

# [Optional] Uncomment this section to install additional OS packages.
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install xz-utils zip bash-completion nano \
    #
    # Provide dependencies for gui application
    && apt-get -y install xorg

# Add autocomplete for git in terminal
RUN echo "source /usr/share/bash-completion/completions/git" > /root/.bashrc
