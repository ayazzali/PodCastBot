# to run: docker build . -t g2 && docker run --rm -it     -e TKEY=123 -e YKEY=321 g2
FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY PodCastBot/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY PodCastBot/. ./
RUN dotnet publish -c Release -o out



# runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /appr
COPY --from=build-env /app/out .
RUN echo pub=$CI_pub_test prv=$CI_prv_test
ENTRYPOINT  echo $CI_pub_test $CI_prv_test \
&& sed -i 's/key2/'$YKEY'/g' cfg.json \
&& dotnet  PodCastBot.dll
