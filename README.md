# YouTubeRIP
Консольное приложение для скачивания видео с платформы YouTube в максимальном качестве

## Как использовать?
> При первом запуске возможно потребуется перезапустить приложение. Будет создан файл Urls.txt и папка Downloaded&Merged.
В файле Urls.txt укажите ссылки на видео для скачивания. Одна ссылка в одну строку
Запустите приложение
На выбор будет предложено 4 действия:
### Начать загрузку
> Ссылки на видео будут взяты из файла Urls.txt
> Следующим шагом введите целочисленное значение, которое укажет программе сколько видео одновременно загружать.
* Прим.
* Максимальная скорость загрузки одного файла 2.5Мбит\с
* При 100Мбит\с интернете:
* > 4 видео загрузят трафик на 90%
* > 5 видео загрузят трафик на 100%
### Скачать файл видео
> Укажите ссылку на видео для загрузки ТОЛЬКО файла видео
### Скачать файл звука
> Укажите ссылку на видео для загрузки ТОЛЬКО файла звука
### Обьединить звук с видео
> Укажите название файла видео с расширением
> Укажите название файла звуковой дорожки с расширением
> Итоговый файл будет помещен в папку Downloaded&Merged

## Как это работает
Платформа YouTube для скачивания видео в максимальном разрешении позволяет получить только файл видео отдельно от звука.
Программа скачивает отдельно 2 файла, а после обьединяет их в одно цельное видео и помещает файл расширения .mp4 в папку Downloaded&Merged

## Доступ к YouTube можно получить
https://github.com/ValdikSS/GoodbyeDPI
