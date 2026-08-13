# MarketFloat

一个透明、始终置顶的 Windows 市场悬浮窗，显示：

- 标普 500（S&P 500）当日曲线、最新点位与涨跌幅
- 比特币兑美元（BTC/USD）当日曲线、最新价格与涨跌幅
- 两分钟自动刷新，也可手动刷新
- 可切换 1天、1周、1月、1年曲线范围，两类资产同步更新
- 点击“—”隐藏到系统托盘；双击托盘图标恢复
- 点击“×”完全退出程序
- 拖动顶部区域可移动窗口
- 曲线显示横轴时间或月份标记，以及纵轴价格刻度

## 运行

在项目目录执行：

```powershell
dotnet run
```

## 生成独立 EXE

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

生成的 `MarketFloat.exe` 可复制到其他 64 位 Windows 电脑直接运行，不要求预装 .NET。

行情优先使用 Yahoo Finance；遇到限流时会自动切换到美联储 FRED（标普 500 日线）和 Coinbase（BTC/USD 五分钟线）。市场休市时，标普 500 会显示最近一个交易日数据；BTC 全天交易。
