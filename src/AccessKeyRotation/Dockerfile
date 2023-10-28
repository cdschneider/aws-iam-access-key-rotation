FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR app/

COPY . ./
RUN dotnet publish -f net7.0 --ucr -o publish/ --no-self-contained

FROM public.ecr.aws/lambda/dotnet:7 AS final
COPY --from=build app/publish/* ${LAMBDA_TASK_ROOT}/

CMD [ "AccessKeyRotation::AccessKeyRotation.Function_FunctionHandler_Generated::FunctionHandler" ]
