FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR app/

COPY . ./
RUN dotnet publish -f net6.0 --ucr -o publish/ --no-self-contained

FROM public.ecr.aws/lambda/dotnet:6 AS final
COPY --from=build app/publish/* ${LAMBDA_TASK_ROOT}/

CMD [ "AccessKeyActions::AccessKeyActions.Function::FunctionHandler" ]
