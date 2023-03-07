ARG TAG
FROM mcr.microsoft.com/dotnet/sdk:${TAG} AS build

ARG OS

WORKDIR /source

COPY . .
RUN dotnet publish -c release -r linux-x64 -o app/${OS}

FROM scratch as export
COPY --from=build /source/app /
