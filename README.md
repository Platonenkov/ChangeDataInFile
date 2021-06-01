# ChangeDataInFile
Производит поиск и замену данных в файле

При первом запуске создаёт файл конфигурации `UpdateFilesScheme`

где

`FileMask` - имя файла или расширение файлов в которых будет происходить поиск
`Updating` - действия с файлом (`Old` - старое значение, `New` - новое значение

UpdateFilesScheme
```Json
[
	{
		"FileMask": "*.csproj",
		"Updating": [
			{
				"Old": "RRJ-Express.ContainerCore, Version=1.1.1.5",
				"New": "RRJ-Express.ContainerCore, Version=1.1.1.6"
			},
			{
				"Old": "RRJ-Express.ContainerCore.1.1.1.5",
				"New": "RRJ-Express.ContainerCore.1.1.1.6"
			},
			{
				"Old": "RRJ-Express.ExpressCore, Version=1.0.0.23",
				"New": "RRJ-Express.ExpressCore, Version=1.0.0.24"
			},
			{
				"Old": "RRJ-Express.ExpressCore.1.0.0.23",
				"New": "RRJ-Express.ExpressCore.1.0.0.24"
			}
		]
	},
	{
		"FileMask": "packages.config",
		"Updating": [
			{
				"Old": "RRJ-Express.ContainerCore\" version=\"1.1.1.5",
				"New": "RRJ-Express.ContainerCore\" version=\"1.1.1.6"
			},
			{
				"Old": "RRJ-Express.ExpressCore\" version=\"1.0.0.23",
				"New": "RRJ-Express.ContainerCore\" version=\"1.0.0.24"
			}
		]
	}
]
```
