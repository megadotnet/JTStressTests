// ============================================================================
// 文件名称: MSTestSettings.cs
// 核心功能: MSTest 单元测试运行框架全局配置，启用方法级别的测试用例并发执行
// 创建时间: 2026-11-05
// 维护人员: megadotnet
// 核心依赖: Microsoft.VisualStudio.TestTools.UnitTesting
// ============================================================================

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
