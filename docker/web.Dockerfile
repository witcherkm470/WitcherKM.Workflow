FROM node:24-alpine AS build
WORKDIR /web
COPY scripts/package.json scripts/package-lock.json ./
RUN npm ci --include=dev
COPY scripts/ ./
RUN npm run build

FROM build AS test
RUN npm run lint

FROM nginx:stable-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist/ /usr/share/nginx/html/
EXPOSE 80
