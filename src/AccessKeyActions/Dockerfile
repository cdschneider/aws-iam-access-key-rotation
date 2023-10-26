FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR app/

COPY . ./
RUN dotnet publish -f net7.0 -r linux-x64 -o publish/ --no-self-contained

FROM public.ecr.aws/lambda/dotnet:7.2023.10.24.16-x86_64
COPY --from=build app/publish/* ${LAMBDA_TASK_ROOT}/

CMD [ "AccessKeyActions::AccessKeyActions.Function_FunctionHandler_Generated::FunctionHandler" ]
