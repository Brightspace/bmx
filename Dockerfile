ARG TAG=7.0
FROM mcr.microsoft.com/dotnet/sdk:${TAG} AS build
ARG OS=debian11

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       clang zlib1g-dev

WORKDIR /source

COPY . .
RUN dotnet publish -c release -r linux-x64 -o app

FROM scratch as export
COPY --from=build /source/app /
