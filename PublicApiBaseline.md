# Public API baseline — 0.2.5

This baseline protects the public ABI from accidental changes. Intentional API changes must update this file and release notes.

Aligned with PCL N host + **PCL.Plugin.Sidecar** product runtime (out-of-process CoreCLR) and in-process ALC plugin loading inside the sidecar.

## Entry and lifecycle

- IPclNPlugin
- IPluginContext
- IPluginLifetime
- IPluginRegistration
- IPluginCapability
- IPluginCapabilityProvider

## Identity and versions

- PluginId
- PluginVersion
- PluginApiVersion
- PluginApiVersionRange
- PluginDescriptor
- PluginServiceId
- PluginDirectorySet

## Core services

- IPluginService
- IPluginServiceProvider
- IPluginLogger
- IPluginDispatcher
- IPluginNotificationService
- IPluginSettingsStore
- IPluginCommandService
- IPluginTaskService
- IPluginTaskRegistration
- IPluginInstanceReadService
- IPluginLocalizationService
- IPluginSecureStorage
- IPluginUriLauncher
- IPluginBackgroundTaskService
- IPluginBackgroundTask
- PluginBackgroundTaskProgress
- PluginBackgroundTaskStep
- PluginBackgroundTaskStepState
- PluginSecretKey
- PluginSecretReadResult
- PluginSecretOperationResult
- PluginSecureStorageStatus
- PluginSettingKey
- PluginCommandDescriptor
- PluginInstanceInfo
- PluginServiceIds

## Extended host services

- IPluginGameSessionService
- IPluginGameOutputService
- IPluginLaunchEventService
- IPluginProcessService
- IPluginClipboardService
- IPluginFileService
- IPluginAccountReadService
- IPluginDownloadService
- IPluginLaunchModificationService
- IPluginPackageAssetService
- PluginGameSessionState
- PluginGameOutputChannel
- PluginGameSessionSnapshot
- PluginGameProcessOutput
- PluginLaunchEvent
- PluginProcessRequest
- PluginProcessResult
- PluginAccountProviderInfo
- PluginDownloadSourceInfo
- PluginLaunchModification
- PluginLaunchRequest
- PluginPackageAssetStatus
- PluginPackageAsset
- PluginPackageAssetResult

## Registry and runtime patches

- IPluginRegistryService
- IPluginRegistryRegistration
- IPluginRuntimePatchService
- PluginRegistryRights
- PluginRegistryAccessRule
- PluginRegistryNode
- PluginRegistryNodeDescriptor
- PluginRegistryChangeKind
- PluginRegistryChange
- PluginRuntimePatchTarget
- PluginRuntimePatchDescriptor
- PluginRuntimePatchInfo

## Exports, health, migration, market

- IPluginExportRegistry
- PluginExportId
- PluginExportDescriptor
- PluginImport
- IPluginDataMigration
- IPluginMigrationContext
- PluginDataClassification
- IPluginHealthCheck
- PluginHealthStatus
- PluginHealthResult
- IPluginMarketClient
- UnconfiguredPluginMarketClient
- PluginMarketAccessFailure
- PluginMarketAccessException
- PluginMarketListQuery
- PluginMarketPluginSummary
- PluginMarketPluginDetail
- PluginMarketVersionInfo
- PluginMarketDependency
- PluginMarketDownloadPart
- PluginMarketDownloadInfo
- PluginMarketPackageVerificationRequest
- PluginMarketPackageVerification
- PluginMarketCategory
- PluginMarketPublisher

## Manifest

- PluginManifest
- PluginPublisherManifest
- PluginAuthorManifest
- PluginEntryPointManifest
- PluginApiRangeManifest
- PluginHostRangeManifest
- PluginPlatformManifest
- PluginDependencyManifest
- PluginIncompatibilityManifest
- PluginServiceRequirementsManifest
- PluginPermissionManifest
- PluginLocalizationManifest
- PluginUiManifest
- PluginAvaloniaRangeManifest
- PluginUiTargetManifest
- PluginUiOperationManifest
- PluginUiPreconditionsManifest
- PluginUiCompatibilityManifest
- PluginDataManifest
- PluginDataMigrationManifest
- PluginActivationManifest
- PluginUpdateManifest
- PluginNativeManifest
- PluginNativeLibraryManifest
- PluginSigningManifest
- PluginSigningPolicyManifest
- PluginManifestJsonContext

## Settings capabilities

- PluginSettingsHintKind
- PluginSettingsHintDescriptor
- PluginSettingsPageGroupDescriptor
- PluginSettingsPageDescriptor
- IPluginSettingsPageGroupCapability
- IPluginSettingsPageCapability
- PluginLocalizedSettingsHintDescriptor
- PluginLocalizedSettingsPageGroupDescriptor
- PluginLocalizedSettingsPageDescriptor
- IPluginLocalizedSettingsPageGroupCapability
- IPluginLocalizedSettingsPageCapability

## UI surfaces and patches

- PluginUiSurfaceKind
- PluginUiOperation
- PluginUiSlotCardinality
- PluginUiSlotDescriptor
- PluginUiSurfaceDescriptor
- IPluginUiSurfaceRegistry
- PluginUiSlotContributionDescriptor
- IPluginUiSurfaceCapability
- PluginUiPatchKind
- PluginUiModifyConflictPolicy
- PluginUiPatchFallback
- PluginUiPatchDescriptor
- PluginUiPatchPreconditions
- PluginUiConflictSeverity
- PluginUiConflictKind
- PluginUiConflict
- PluginUiPatchPlan
- IPluginUiPatchService

## UI adapter packages (PCLN.Plugin.UI / UI.Avalonia)

- PclLocalizedString
- UiTargetId
- PluginPageDescriptor
- IPluginNavigationService
- IPluginUiDispatcher
- PclUiString
- PclUiElement
- PclUiText
- PclUiButton
- PclUiTextBox
- PclUiToggle
- PclUiSelect
- PclUiOption
- PclUiSlider
- PclUiProgress
- PclUiCard
- PclUiStack
- PclUiSpacer
- PclUiMarkdown
- PclUiThickness
- PclUiOrientation
- PclUiTextStyle
- PclUiButtonStyle
- PclUiPage
- PclUiContribution
- PclUiEventKind
- PclUiEventArgs
- IAvaloniaUiAccessService
- IAvaloniaUiContext
- IUiTargetHandle
- IAvaloniaPluginPageService
- IAvaloniaPluginWindowService
- AvaloniaPluginPageDescriptor
- AvaloniaPluginWindowDescriptor

No type from PCL.Application, PCL.Desktop, or private PCL.Plugin is part of this baseline.
