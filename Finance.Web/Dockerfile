FROM node:20-alpine AS build
WORKDIR /app

COPY Finance.Web/package.json Finance.Web/package-lock.json ./
RUN npm ci

COPY Finance.Web/ ./
RUN npm run build -- --output-path=dist/browser

FROM nginx:1.27-alpine
COPY Finance.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/browser/ /usr/share/nginx/html/

EXPOSE 80