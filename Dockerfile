# to run: docker build . -t g2 && docker run --rm -it     -e TKEY=123 -e YKEY=321 g2
FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
# COPY src/PodCastBot/*.csproj ./
COPY / ./
RUN dotnet restore

# Copy everything else and build
# COPY src/. ./

# правда обслоютно всё будет в одной папке 
RUN dotnet publish -c Release -o ../../out 



# runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /appr
COPY --from=build-env /app/out .
# --from=build-env /app/src/PodCastBot/out .
ENTRYPOINT  echo 0$CI_pub_test 1$CI_prv_test 0$Test1 1$Test2\
&& sed -i 's/267989730:AAH7VbASzQeOLWf8iLSdusooE00Pg_qlao4/'$TKEY'/g' cfg.json \
&& dotnet  PodCastBot.dll
