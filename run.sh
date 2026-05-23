#!/usr/bin/env bash
# ============================================================
#   Caderno Vivo – script de inicialização (Linux / macOS)
# ============================================================
set -e

cd "$(dirname "$0")"

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║           📓  Caderno Vivo               ║"
echo "╚══════════════════════════════════════════╝"
echo ""

# 1. Restaurar pacotes NuGet
echo "▶ [1/3] Restaurando pacotes..."
dotnet restore --nologo -q
echo "    OK"

# 2. Aplicar migrations / criar banco
#    O app usa EnsureCreated(), então o banco é criado automaticamente
#    na primeira execução. Para migrations explícitas, descomente abaixo:
#
#    if ! command -v dotnet-ef &>/dev/null; then
#        echo "    Instalando dotnet-ef..."
#        dotnet tool install --global dotnet-ef
#    fi
#    if [ ! -d "Data/Migrations" ] || [ -z "$(ls -A Data/Migrations 2>/dev/null)" ]; then
#        echo "    Criando migration inicial..."
#        dotnet ef migrations add Inicial --output-dir Data/Migrations -q
#    fi
#    dotnet ef database update -q

echo "▶ [2/3] Banco de dados (criado automaticamente ao iniciar)"

# 3. Iniciar servidor
echo "▶ [3/3] Iniciando servidor..."
echo ""
echo "  ┌─────────────────────────────────────────┐"
echo "  │  Acesse: http://localhost:5000          │"
echo "  │  Pressione Ctrl+C para encerrar        │"
echo "  └─────────────────────────────────────────┘"
echo ""

dotnet run --urls "http://localhost:5000"
