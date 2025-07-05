#!/bin/bash
logPath=/var/log/oneswiss
techLogPath=$logPath/techlog
userName=oneswiss

if [[ $(getent passwd userName) = "" ]]; then
  echo 'Создание пользователя oneswiss'
  sudo adduser --system --no-create-home --disabled-login $userName
else
  echo "Пользователь $userName уже существует"
fi

echo ''
echo 'Создание каталога сбора технологического журнала'

if [ -d "techLogPath" ]; then
  echo "Каталог сбора технологического журнала уже существует ($techLogPath)"
else
  sudo mkdir -p "$techLogPath"
  
  echo "Предоставить полные права (777) на каталог сбора технологического журнала?
Если 'n', то запись журнала клиентских приложений выполняться в него не будет (770),
а владельцем каталога будет назначен пользователь oneswiss и группа пользователя, запускающего службу агента сервера 1С"
  
  read -p "По умолчанию - Y. Y/n? " yn
  if [ "yn" != "${yn#[Yy]}" ] ;then 
   sudo chmod 777 "$techLogPath"
  else
   echo "Укажите группу пользователя, запускающего службу агента сервера 1С"    
   # shellcheck disable=SC2162
   read -p 'По умолчанию - grp1cv8:' grp1c
   if [ "$grp1c" == "" ] 
   then
     grp1c='grp1cv8'
   fi
    
   sudo chown "$userName:$grp1c" "$techLogPath" 
   sudo chmod 770 "$techLogPath" 
  fi
fi

echo ''
echo "Регистрация службы OneSwiss Agent"
sudo systemctl link "$(dirname "$0")/oneswiss-agent.service"
sudo systemctl enable oneswiss-agent.service