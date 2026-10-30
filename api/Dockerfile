FROM node:22-alpine
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci --omit=dev || npm install --omit=dev
COPY src ./src
COPY migraciones ./migraciones
COPY test ./test
ENV NODE_ENV=production
EXPOSE 3000
CMD ["node", "src/indice.js"]
