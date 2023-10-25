FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR app/

COPY src/AccessKeyRotation/ .
RUN dotnet tool restore
RUN dotnet lambda package 

FROM public.ecr.aws/lambda/dotnet:6
COPY publish/* ${LAMBDA_TASK_ROOT}

# Set the CMD to your handler (could also be done as a parameter override outside of the Dockerfile)
CMD [ "MyProject::MyFunction.Function::FunctionHandler" ]
