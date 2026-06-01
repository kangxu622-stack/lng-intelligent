# 纯运行版，不编译、不发布
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# 工作目录
WORKDIR /app

# 复制所有发布好的文件
COPY . .

# 端口
EXPOSE 80

# 启动 .NET 后端
ENTRYPOINT ["dotnet", "backend.dll"]
