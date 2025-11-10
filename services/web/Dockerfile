# Dockerfile para desenvolvimento com ng serve
FROM node:20-alpine

WORKDIR /app

# Copiar arquivos de dependências
COPY package*.json ./

# Instalar dependências
RUN npm install

# Copiar código fonte
COPY . .

# Expor porta 4200
EXPOSE 4200

# Comando para rodar ng serve com host 0.0.0.0 (necessário para Docker)
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--poll", "2000"]
