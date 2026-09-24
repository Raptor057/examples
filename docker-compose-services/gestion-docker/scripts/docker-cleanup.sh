#!/bin/bash
echo "📊 Espacio antes de limpiar:"
df -h /

echo -e "\n🧹 Limpiando Docker (recursos no utilizados)..."
docker system prune -a --volumes -f

echo -e "\n📊 Espacio después de limpiar:"
df -h /

echo -e "\n✅ Limpieza completada."


