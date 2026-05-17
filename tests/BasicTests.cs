using Xunit;

namespace MzmChar.Tests;

/// <summary>
/// 占位测试 —— 主项目的所有内容（角色 / 卡 / 遗物 / 池子）都继承 BaseLib 的 CustomXxxModel，
/// 这些类的 ctor 依赖游戏运行时上下文（CustomContentDictionary 等），无法离线 instantiate 测试。
/// 真正的功能验证发生在游戏里：启动 → 加载 mod → 选角 → 进战斗。
///
/// 保留这个测试项目结构是为了未来如果有纯逻辑（数学计算、配置校验、本地化字典 lint 等）
/// 可以离线测的话，框架已经在这里了。
/// </summary>
public class ScaffoldTests
{
    [Fact]
    public void TestProjectCompiles()
    {
        Assert.True(true);
    }
}
