@echo off
REM ============================================================
REM   Caderno Vivo – script de inicializacao (Windows)
REM ============================================================
cd /d "%~dp0"

echo.
echo ╔══════════════════════════════════════════╗
echo ║           Caderno Vivo                   ║
echo ╚══════════════════════════════════════════╝
echo.

REM 1. Restaurar pacotes
echo [1/3] Restaurando pacotes...
dotnet restore --nologo -q
echo     OK

REM 2. Banco criado automaticamente pela aplicacao
echo [2/3] Banco de dados (criado automaticamente ao iniciar)

REM 3. Iniciar servidor
echo [3/3] Iniciando servidor...
echo.
echo   Acesse: http://localhost:5000
echo   Pressione Ctrl+C para encerrar
echo.

dotnet run --urls "http://localhost:5000"
