# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.231.6/containers/debian/.devcontainer/base.Dockerfile

# [Choice] Debian version (use bullseye on local arm64/Apple Silicon): bullseye, buster
ARG VARIANT="buster"
# https://github.com/microsoft/vscode-dev-containers/tree/main/containers/java
FROM mcr.microsoft.com/vscode/devcontainers/base:0-${VARIANT}

# ** [Optional] Uncomment this section to install additional packages. **
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends graphviz xclip

WORKDIR /usr/local/bin

ADD https://github.com/npryce/adr-tools/archive/3.0.0.tar.gz adr-tools.tar.gz
RUN sudo tar -xzvf adr-tools.tar.gz adr-tools-3.0.0/src
RUN sudo rm adr-tools.tar.gz

ENV PATH="/usr/local/bin/adr-tools-3.0.0/src:${PATH}"