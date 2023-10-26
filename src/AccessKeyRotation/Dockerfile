FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR app/

COPY . ./
RUN dotnet publish -r linux-x64 -o publish/ --no-self-contained

FROM public.ecr.aws/lambda/dotnet:6-x86_64
COPY --from=build app/publish/* ${LAMBDA_TASK_ROOT}/

CMD [ "AccessKeyRotation::AccessKeyRotation.Function_FunctionHandler_Generated::FunctionHandler" ]
