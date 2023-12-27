FROM mcr.microsoft.com/dotnet/sdk:6.0.417 AS build
WORKDIR app/

COPY . ./
RUN dotnet publish -f net6.0 --use-current-runtime -o publish/ --no-self-contained

FROM public.ecr.aws/lambda/dotnet:6.2023.10.16.09 AS final
COPY --from=build app/publish/* ${LAMBDA_TASK_ROOT}/

CMD [ "AccessKeyActions::AccessKeyActions.Function_FunctionHandler_Generated::FunctionHandler" ]
