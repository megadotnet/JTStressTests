# ==============================================================================
# Multi-stage Cloud-Native Dockerfile for JTStressTests Web Application
# Compliant with OCI image standards, CIS benchmark, and Kubernetes cloud-native deployment
# ==============================================================================

# ------------------------------------------------------------------------------
# Stage 1: Build & Publish Stage (using official lightweight .NET 8 SDK Alpine)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

WORKDIR /src

# 1. 复制工程文件先单独还原依赖，利用 Docker 构建缓存
COPY JTStressTests.sln ./
COPY JTStressTests.csproj ./
COPY JTStressTests.Tests/JTStressTests.Tests.csproj ./JTStressTests.Tests/

RUN dotnet restore JTStressTests.csproj -r linux-musl-x64

# 2. 复制全部源代码并执行 Release 编译发布
COPY . .

RUN dotnet publish JTStressTests.csproj \
    -c Release \
    -r linux-musl-x64 \
    --no-restore \
    --self-contained false \
    -o /app/publish

# ------------------------------------------------------------------------------
# Stage 2: Minimal Production Runtime Stage (using official lightweight ASP.NET 8 Alpine)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

# OCI 镜像元数据标准声明
LABEL org.opencontainers.image.title="JTStressTests" \
      org.opencontainers.image.description="Cloud-Native HTTP Load & Stress Testing Web Service" \
      org.opencontainers.image.version="1.0.0" \
      org.opencontainers.image.authors="JTStressTests Team" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.source="https://github.com/your-org/JTStressTests"

# 设置工作目录
WORKDIR /app

# 安装必要的运行时组件（wget 用于健康检查），并清理 apk 缓存
RUN apk add --no-cache wget ca-certificates tzdata \
    && rm -rf /var/cache/apk/*

# 创建非 root 专用运行用户和用户组（UID 10001, GID 10001）
RUN addgroup -g 10001 -S appuser \
    && adduser -u 10001 -S appuser -G appuser

# 从构建阶段复制发布产物
COPY --from=builder --chown=appuser:appuser /app/publish /app

# 创建持久化/输出目录并配置严格的权限
RUN mkdir -p /app/output \
    && chown -R appuser:appuser /app \
    && chmod -R 550 /app \
    && chmod -R 770 /app/output

# 切换为非 root 用户运行
USER 10001:10001

# 设置云原生环境变量
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    PORT=8080 \
    WEB_SERVER=true

# 明确声明暴露的服务端口
EXPOSE 8080

# 配置容器健康检查指令（HEALTHCHECK），适配 Kubernetes 等平台探活要求
HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8080/healthz || exit 1

# 容器启动入口点
ENTRYPOINT ["dotnet", "/app/JTStressTests.dll"]
CMD ["--server", "--port", "8080"]
