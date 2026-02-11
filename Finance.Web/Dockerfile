FROM node:20-alpine AS build
WORKDIR /app

COPY Finance.Web/client-app/package.json Finance.Web/client-app/package-lock.json ./
RUN npm ci

COPY Finance.Web/client-app/ ./
RUN npm run build -- --output-path=dist/browser

FROM nginx:1.27-alpine
COPY Finance.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/browser/ /usr/share/nginx/html/

EXPOSE 80