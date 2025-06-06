#### Install

```ps1
Install-Package OutWit.Common.MessagePack
```

or

```bash
> dotnet add package OutWit.Common.MessagePack
```

#### Serialize to message pack bytes

```C#
MyData data1 = new MyData();

var bytes = data1.ToPackBytes();

var data2 = bytes.FromPackBytes<MyData>();
```

or

```C#
MyData data1 = new MyData();

var bytes = data1.ToPackBytes();

var data2 = bytes.FromPackBytes(typeof(MyData));
```

#### Serialize to shared memory-mapped file

```C#
MyData data1 = new MyData();

data1.ToPackMemoryMappedFile(out string mapName, out int length);

var data2 = mapName.FromPackMemoryMappedFile<MyData>(length);
```

or

```C#
MyData data1 = new MyData();

data1.ToPackMemoryMappedFile(out string mapName, out int length);

var data2 = FromPackMemoryMappedFile(length, typeof(MyData));
```
