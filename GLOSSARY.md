# 领域词汇表

RszTool 是 RE ENGINE 资源文件的读写工具。下面这些词在代码和讨论里都按本表的意思使用，括号里是承载它的模块。

## 文件

- **资源文件（resource file）**：一个 `.scn` / `.pfb` / `.user` 文件，由「资源表」加若干「实例」组成。基类是 `BaseRszFile`（`RszTool/RszFile/BaseRszFile.cs`）。
- **场景文件（scene file，`.scn`）**：装着「文件夹」和「游戏对象」的树，另有资源表与 info 表。（`RszTool/RszFile/ScnFile.cs`）
- **预制体文件（prefab file，`.pfb`）**：只有游戏对象树和资源表，没有文件夹层。（`RszTool/RszFile/PfbFile.cs`）
- **用户文件（user file，`.user`）**：单对象的资源文件。（`RszTool/RszFile/UserFile.cs`）

## 实例与类型

- **实例（instance）**：一个 RSZ 类的实例，字段值存在 `Values` 里，类型信息来自类定义。见 `RszTool/RszFile/RszInstance.cs`。
- **类定义（class definition）**：`rsz<game>.json` 描述的全部 RSZ 类，由 `RszParser` 持有。
- **枚举表（enum table）**：`Data/Enums/<game>_enum.json`，由 `EnumParser` 持有。
- **文件选项（file option）**：打开一个文件所需的全部外部依赖——游戏名、版本、类定义、枚举表。由 `RszFileOption`（`RszTool/RszFile/RszFileOption.cs`）显式传入，不读全局注册表。

## 游戏对象图

- **游戏对象（game object）**：场景/预制体里的一个节点，有自己的实例、组件列表和子对象。（`IGameObjectData` 及两个派生实现）
- **组件（component）**：挂在游戏对象上的实例。
- **游戏对象图（game-object graph）**：对象、组件、子对象组成的整棵树。
- **游戏对象树模块（game-object tree module）**：对这张图的遍历与查询，与具体文件格式无关——收集实例、遍历对象、找 `chainsaw.ContextID` 引用。见 `RszTool/RszFile/GameObjectTree.cs`。任何需要「按树走一遍」的代码都走这里，不要在 `.scn` / `.pfb` 里各写一份。

## 编辑

- **编辑入口（edit seam）**：`BaseRszFileViewModel.OnEdited` / `OnInserted` / `OnGameObjectInserted`。所有「文件已被改动」的动作都从这里过：它负责置脏标记、刷新受影响的节点，并按需做 `ContextID` 修正。
- **节点刷新（items provider）**：`IItemsProvider.NotifyItemsChanged`（`RszTool.App/ViewModels/IItemsProvider.cs`）。`OnEdited` 拿到节点后调它，节点自己决定要不要重建列表。
- **剪贴板（clipboard）**：全局唯一的复制来源，`RszClipboard`（`RszTool.App/ViewModels/RszClipboard.cs`）。实例、普通字段值、游戏对象都在这里，跨 `.scn` / `.pfb` 的粘贴转换也由它负责。
- **ContextID 修正（context-id fixup）**：粘贴或新建后修正 re4 的 `chainsaw.ContextID` 字段。`ContextIdFixup`（`RszTool.App/ViewModels/ContextIdFixup.cs`）只在 `re4` 生效，界面入口是 `Prompt` 委托，没有界面时静默跳过。
- **info 表（info table）**：保存时重建的对象索引表，`.scn` / `.pfb` / `.user` 各有自己的重建策略（`RebuildInfoTable`）。

## 界面

- **工作区文件夹（workspace folders）**：左侧文件树里用户添加的根目录，持久化在 `SaveData.OpenedFolders`。
- **目录列举（folder listing）**：把一个目录读成「目录在前、各自按名字排序」的条目列表，与界面状态无关。（`RszTool.App/Common/FolderListing.cs`）
- **目录节点（directory item）**：文件树里的一个节点，只管展开状态和子节点集合，内容按需向「目录列举」要。（`RszTool.App/ViewModels/FileExplorerViewModel.cs`）