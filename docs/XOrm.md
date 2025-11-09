# XOrm

[![NuGet](https://img.shields.io/nuget/v/EFramework.DotNet.CRUD.svg?label=NuGet)](https://www.nuget.org/packages/EFramework.DotNet.CRUD)
[![DeepWiki](https://img.shields.io/badge/DeepWiki-Explore-blue)](https://deepwiki.com/eframework-io/DotNet.CRUD)
[![Discord](https://img.shields.io/discord/1422114598835851286?label=Discord&logo=discord)](https://discord.gg/XMPx2wXSz3)

拓展了 SqlSugar 的 ORM 功能，同时实现了基于上下文的事务机制，提高了数据操作的效率。

## 功能特性

- 多源配置：通过首选项中的配置自动初始化数据库连接
- 事务操作：基于上下文的事务机制，支持缓存和并发控制

## 使用手册

### 1. 初始化

#### 1.1 单数据库

使用标准的 ADO.NET 连接字符串格式配置数据库：

```csharp
var preferences = new XPrefs.IBase();
var config = new XPrefs.IBase();
config.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
preferences.Set("XOrm/Source/MySQL/myalias", config);
XOrm.Initialize(preferences);
```

#### 1.2 多数据库

支持配置多个数据库连接：

```csharp
var preferences = new XPrefs.IBase();
var config1 = new XPrefs.IBase();
config1.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
preferences.Set("XOrm/Source/MySQL/myalias1", config1);
var config2 = new XPrefs.IBase();
config2.Set(XOrm.Preferences.Addr, "server=localhost;port=3306;database=mysql;uid=root;pwd=123456;");
preferences.Set("XOrm/Source/MySQL/myalias2", config2);
XOrm.Initialize(preferences);
```

#### 1.3 DSN 格式

支持 DSN 格式的连接字符串，会自动转换为 ADO.NET 格式：

```csharp
var preferences = new XPrefs.IBase();
var config = new XPrefs.IBase();
config.Set(XOrm.Preferences.Addr, "root:123456@tcp(127.0.0.1:3306)/mysql?charset=utf8mb4&loc=Local");
preferences.Set("XOrm/Source/MySQL/myalias", config);
XOrm.Initialize(preferences);
// 会自动转换为：server=127.0.0.1;port=3306;database=mysql;uid=root;pwd=123456;
```

### 2. 上下文

#### 2.1 开始

使用 Watch 开始一个上下文，所有后续操作将在同一个事务中执行：

```csharp
XOrm.Watch();
```

#### 2.2 结束

使用 Defer 结束上下文并提交事务：

```csharp
XOrm.Defer();
```

上下文会自动记录所有 CRUD 操作的 SQL 语句和耗时信息。

### 3. 插入数据

#### 3.1 单个对象

```csharp
var model = new MyModel()
{
    IntVal = 1,
    FloatVal = 1.0f,
    StringVal = "model1",
    BoolVal = true
};
var affect = XOrm.Insert<MyModel>(model);
```

#### 3.2 对象列表

```csharp
var models = new List<MyModel>()
{
    new MyModel() { IntVal = 1, StringVal = "model1" },
    new MyModel() { IntVal = 2, StringVal = "model2" }
};
var affect = XOrm.Insert<MyModel>(models);
```

#### 3.3 自定义操作

```csharp
var affect = XOrm.Insert<MyModel>(model, insertable => 
{
    insertable.IgnoreColumns(e => new { e.ID });
});
```

### 4. 查询数据

#### 4.1 条件查询

```csharp
var model1 = XOrm.Query<MyModel>(queryable => 
    queryable.Where(e => e.ID == 1)
).Where(e => e.StringVal == "model1").First();
```

#### 4.2 全量查询

```csharp
var models = XOrm.Query<MyModel>().ToList();
```

#### 4.3 链式查询

```csharp
var models = XOrm.Query<MyModel>()
    .Where(e => e.IntVal > 100)
    .OrderBy(e => e.ID)
    .Take(10)
    .ToList();
```

### 5. 更新数据

#### 5.1 条件更新

更新指定字段：

```csharp
var model = XOrm.Query<MyModel>().Where(e => e.ID == 1).First();
model.IntVal = -1;
model.FloatVal = -1.0f;
var affect = XOrm.Update<MyModel>(model, updateable => 
    updateable.UpdateColumns(e => new { e.IntVal, e.FloatVal })
);
```

#### 5.2 全量更新

更新所有字段：

```csharp
var model = new MyModel() { ID = 2, FloatVal = -1.0f };
var affect = XOrm.Update<MyModel>(model);
```

#### 5.3 表达式更新

```csharp
var affect = XOrm.Update<MyModel>(
    e => e.ID == 1,
    updateable => updateable.SetColumns(e => new MyModel() 
    { 
        IntVal = 100,
        StringVal = "updated"
    })
);
```

### 6. 删除数据

#### 6.1 实例删除

```csharp
var model = XOrm.Query<MyModel>().Where(e => e.ID == 1).First();
var affect = XOrm.Delete<MyModel>(model);
```

#### 6.2 条件删除

```csharp
var affect = XOrm.Delete<MyModel>(onDelete: deleteable => 
    deleteable.Where(e => e.ID == 2)
);
```

#### 6.3 主键删除

```csharp
var affect = XOrm.Delete<MyModel>(3);
```

#### 6.4 全量删除

```csharp
var affect = XOrm.Delete<MyModel>();
```

## 常见问题

### 1. 如何配置数据库连接？

通过 XPrefs 配置数据库连接，配置键格式为：`XOrm/Source/{数据库类型}/{别名}`。
数据库类型会自动识别，只要枚举值的 ToString 包含配置中的类型字符串即可。

### 2. 支持哪些数据库类型？

支持所有 SqlSugar 支持的数据库类型，包括 MySQL、SQL Server、PostgreSQL、Oracle 等。

### 3. DSN 格式和 ADO.NET 格式有什么区别？

- DSN 格式：`root:123456@tcp(127.0.0.1:3306)/mysql?charset=utf8mb4`
- ADO.NET 格式：`server=127.0.0.1;port=3306;database=mysql;uid=root;pwd=123456;`

XOrm 会自动识别并转换 DSN 格式为 ADO.NET 格式。

### 4. Watch 和 Defer 的作用是什么？

Watch 开始一个上下文，所有后续操作会在同一个事务中执行，并记录操作耗时。
Defer 结束上下文并提交事务，会自动输出性能统计信息。

### 5. 如何查看操作性能统计？

在 Defer 时会自动输出性能统计信息，包括 Insert、Query、Update、Delete 的次数和耗时。

更多问题，请查阅[问题反馈](../CONTRIBUTING.md#问题反馈)。

## 项目信息

- [更新记录](../CHANGELOG.md)
- [贡献指南](../CONTRIBUTING.md)
- [许可协议](../LICENSE)
