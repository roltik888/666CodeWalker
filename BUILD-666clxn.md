# 666clxn 0.8 / Scene Studio

Windows, .NET SDK 8:

    dotnet build CodeWalker/CodeWalker.csproj -c Release -o dist
    dotnet run --project Tests/ClxnModelTests.csproj -c Release

Скопировать Shaders, icons, Notice.txt, Readme_Src.txt и README-RU.txt в dist.
Запуск: Windows 10/11 x64, .NET Framework 4.8, DirectX 11, локальные файлы GTA V.

Основные изменения:
- ClxnControls / ClxnHomeForm: монохромная тема, округления и плавное наведение.
- ClxnWorldHud: контекстные настройки, панель объектов, скрытая служебная статистика.
- ClxnStudioPanels: панель кадров, компактный журнал, меню дополнительных функций.
- ClxnCinematics: запись/редактирование кадров, предпросмотр, пауза, чистый вид.
- ClxnFlightMath: интерполяция, короткий поворот yaw, время и FOV.
- ClxnJournal: ограниченный потокобезопасный буфер сообщений.
- ClxnDocument: обратная совместимость .clxn, необязательный XML-узел flight.
- Rendering/DirectX/DXManager и WorldForm.CleanupScene: остановка потоков перед освобождением GPU-ресурсов.
- ClxnActors: стандартные образы и имя локально доступного ped.

Предпросмотр обновляется внутри RenderSyncRoot; UI меняется только в UI-потоке.
На воспроизведение копируется неизменяемый список кадров.
Игровые файлы, конфигурация пользователя, bin/obj и ключи не включены.
CodeWalker by dexyfex, commit 485d56bec00262ed7fa472261cce7bbc6202b96e.

MP4: ClxnVideoExport (D3D11 resolve/staging readback), ClxnMp4Encoder
(ограниченная очередь, четыре переиспользуемых RGBA-буфера, FFmpeg),
ClxnVideoPlan (длительность и время кадра). ClxnPerformance — профили и кеши.
Для экспорта нужен tools/ffmpeg.exe. В готовом Windows-архиве он включён.
Для собственной сборки: https://ffmpeg.org/download.html → Windows builds gyan.dev.
Положите ffmpeg.exe в dist/tools, сохраните LICENSE и README поставщика.
Версия проверенного кодировщика: 9.0.2 essentials; без изменений.

Интеграционные проверки кодировщика:
    dotnet run --project Tests/ClxnModelTests.csproj -c Release -- C:/path/ffmpeg.exe C:/path/test.mp4

Автоматический тест 3D-экспорта:
    666clxn.exe world --annotations="scene.clxn" --export-mp4="flight.mp4" --export-width=1280 --export-height=720 --export-fps=30 --exit-after-export
