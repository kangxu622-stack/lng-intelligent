# 纯运行版，不编译、不打包
FROM nginx:alpine

# 复制打包好的 dist 到 Nginx
COPY ./dist /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

# 端口
EXPOSE 80

# 启动 Nginx
CMD ["nginx", "-g", "daemon off;"]