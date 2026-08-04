# Home

> Applies to PCL N Plugin SDK 0.2.5.

This page is the English counterpart of the matching Chinese SDK guide. It documents the same contracts, examples, and compatibility requirements.

## Product runtime

PCL N Desktop is **AOT** and loads plugins only via **PCL.Plugin.Sidecar** (CoreCLR, IPC). Plugin ABI remains `IPclNPlugin` + public NuGet packages. Read [Architecture and Boundaries](Architecture-and-Boundaries).

## SDK 0.2.5 requirements

- Target .NET 10 and reference only public PCLN.Plugin packages.
- Provide both locales/zh-CN.json and locales/en-US.json through the localization resource path.
- Declare required and optional permissions explicitly and degrade gracefully when optional capabilities are unavailable.
- Keep the package manifest icon separate from the PNG, JPEG, or WebP market icon uploaded in the publisher workbench.
- Submit Chinese and English market name, summary, and description before review or publishing.

## Contract parity

JSON, C#, AXAML, signing, Testing Host, and publishing behavior are identical across languages. See the SDK source schemas and examples for copy-ready contracts.
