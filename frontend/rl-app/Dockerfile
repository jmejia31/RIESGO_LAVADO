# syntax=docker/dockerfile:1

FROM node:24.18.0-alpine3.24 AS build
WORKDIR /app
COPY frontend/rl-app/package.json frontend/rl-app/package-lock.json ./
RUN npm ci --ignore-scripts
COPY frontend/rl-app/ ./
RUN npm run build -- --configuration production

FROM nginx:1.31.3-alpine AS runtime
RUN rm -f /etc/nginx/conf.d/default.conf \
    && mkdir -p /tmp/nginx/client_temp /tmp/nginx/proxy_temp /tmp/nginx/fastcgi_temp /tmp/nginx/uwsgi_temp /tmp/nginx/scgi_temp \
    && chown -R nginx:nginx /tmp/nginx
COPY frontend/rl-app/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /app/dist/rl-app/browser/ /usr/share/nginx/html/
RUN chown -R root:root /usr/share/nginx/html && chmod -R 755 /usr/share/nginx/html
EXPOSE 8080
USER nginx
HEALTHCHECK --interval=30s --timeout=5s --start-period=5s --retries=3 \
  CMD wget -q -O - http://127.0.0.1:8080/healthz >/dev/null || exit 1
CMD ["nginx", "-g", "daemon off;"]
