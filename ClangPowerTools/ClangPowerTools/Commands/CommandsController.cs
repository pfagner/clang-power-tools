﻿using ClangPowerTools.Commands;
using ClangPowerTools.Events;
using ClangPowerTools.Handlers;
using ClangPowerTools.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using Task = System.Threading.Tasks.Task;

namespace ClangPowerTools
{
  /// <summary>
  /// Contains all the logic of disable and enable the clang custom commands  
  /// </summary>
  public class CommandsController
  {
    #region Members

    public static readonly Guid mCommandSet = new Guid("498fdff5-5217-4da9-88d2-edad44ba3874");
    private AsyncPackage mAsyncPackage;
    private Commands2 mCommand;
    private bool mSaveCommandWasGiven = false;
    private Document mDocument;
    private bool mFormatAfterTidyFlag = false;

    public event EventHandler<VsHierarchyDetectedEventArgs> HierarchyDetectedEvent;

    public event EventHandler<ClangCommandMessageEventArgs> ClangCommandMessageEvent;

    public event EventHandler<MissingLlvmEventArgs> MissingLlvmEvent;

    public event EventHandler<ClearErrorListEventArgs> ClearErrorListEvent;


    #endregion


    #region Properties

    /// <summary>
    /// Store the command id of the current running command
    /// If no command is running then it will have a value less then 0
    /// </summary>
    public int CurrentCommand { get; private set; }

    /// <summary>
    /// Running flag for clang commands
    /// </summary>
    public bool Running { get; set; } = false;


    /// <summary>
    /// Running flag for Visual Studio build
    /// </summary>
    public bool VsBuildRunning { get; set; }

    #endregion


    #region Constructor

    public CommandsController(AsyncPackage aAsyncPackage)
    {
      mAsyncPackage = aAsyncPackage;

      if (VsServiceProvider.TryGetService(typeof(DTE), out object dte))
      {
        var dte2 = dte as DTE2;
        mCommand = dte2.Commands as Commands2;
      }
    }

    #endregion


    #region Public Methods

    public async Task InitializeCommandsAsync(AsyncPackage aAsyncPackage)
    {
      if (CompileCommand.Instance == null)
      {
        await CompileCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kCompileId);
        await CompileCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kCompileToolbarId);
      }

      if (TidyCommand.Instance == null)
      {
        await TidyCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kTidyId);
        await TidyCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.TidyToolbarId);
        await TidyCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kTidyFixId);
        await TidyCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kTidyFixToolbarId);
      }

      if (ClangFormatCommand.Instance == null)
      {
        await ClangFormatCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kClangFormat);
        await ClangFormatCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kClangFormatToolbarId);
      }

      if (IgnoreFormatCommand.Instance == null)
      {
        await IgnoreFormatCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kIgnoreFormatId);
      }

      if (IgnoreCompileCommand.Instance == null)
      {
        await IgnoreCompileCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kIgnoreCompileId);
      }

      if (StopClang.Instance == null)
      {
        await StopClang.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kStopClang);
      }

      if (SettingsCommand.Instance == null)
      {
        await SettingsCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kSettingsId);
      }

      if (TidyConfigCommand.Instance == null)
      {
        await TidyConfigCommand.InitializeAsync(this, aAsyncPackage, mCommandSet, CommandIds.kITidyExportConfigId);
      }
    }

    public async void Execute(object sender, EventArgs e)
    {
      if (!(sender is OleMenuCommand command))
        return;

      if (Running && command.CommandID.ID != CommandIds.kStopClang)
        return;

      switch (command.CommandID.ID)
      {
        case CommandIds.kSettingsId:
          {
            CurrentCommand = CommandIds.kSettingsId;
            SettingsCommand.Instance.ShowSettings();
            break;
          }
        case CommandIds.kStopClang:
          {
            CurrentCommand = CommandIds.kStopClang;
            await StopClang.Instance.RunStopClangCommandAsync();
            break;
          }
        case CommandIds.kClangFormat:
          {
            CurrentCommand = CommandIds.kClangFormat;
            ClangFormatCommand.Instance.RunClangFormat(CommandUILocation.ContextMenu);
            break;
          }
        case CommandIds.kClangFormatToolbarId:
          {
            CurrentCommand = CommandIds.kClangFormat;
            ClangFormatCommand.Instance.RunClangFormat(CommandUILocation.Toolbar);
            break;
          }
        case CommandIds.kCompileId:
          {
            OnBeforeClangCommand(CommandIds.kCompileId);
            await CompileCommand.Instance.RunClangCompileAsync(CommandIds.kCompileId, CommandUILocation.ContextMenu);
            OnAfterClangCommand();
            break;
          }
        case CommandIds.kCompileToolbarId:
          {
            OnBeforeClangCommand(CommandIds.kCompileId);
            await CompileCommand.Instance.RunClangCompileAsync(CommandIds.kCompileId, CommandUILocation.Toolbar);
            OnAfterClangCommand();
            break;
          }
        case CommandIds.kTidyId:
          {
            OnBeforeClangCommand(CommandIds.kTidyId);
            await TidyCommand.Instance.RunClangTidyAsync(CommandIds.kTidyId, CommandUILocation.ContextMenu);
            OnAfterClangCommand();
            break;
          }
        case CommandIds.TidyToolbarId:
          {
            OnBeforeClangCommand(CommandIds.kTidyId);
            await TidyCommand.Instance.RunClangTidyAsync(CommandIds.kTidyId, CommandUILocation.Toolbar);
            OnAfterClangCommand();
            break;
          }
        case CommandIds.kTidyFixId:
          {
            OnBeforeClangCommand(CommandIds.kTidyFixId);
            await TidyCommand.Instance.RunClangTidyAsync(CommandIds.kTidyFixId, CommandUILocation.ContextMenu);
            OnAfterClangCommand();
            break;
          }
        case CommandIds.kTidyFixToolbarId:
          {
            OnBeforeClangCommand(CommandIds.kTidyFixId);
            await TidyCommand.Instance.RunClangTidyAsync(CommandIds.kTidyFixId, CommandUILocation.Toolbar);
            OnAfterClangCommand();
            break;
          }
        case CommandIds.kITidyExportConfigId:
          {
            CurrentCommand = CommandIds.kITidyExportConfigId;
            TidyConfigCommand.Instance.ExportConfig();
            break;
          }

        case CommandIds.kIgnoreFormatId:
          {
            CurrentCommand = CommandIds.kIgnoreFormatId;
            IgnoreFormatCommand.Instance.RunIgnoreFormatCommand(CommandIds.kIgnoreFormatId);
            break;
          }
        case CommandIds.kIgnoreCompileId:
          {
            CurrentCommand = CommandIds.kIgnoreCompileId;
            IgnoreCompileCommand.Instance.RunIgnoreCompileCommand(CommandIds.kIgnoreCompileId);
            break;
          }
        default:
          break;
      }
    }

    #endregion


    #region Private Methods

    private void OnBeforeClangCommand(int aCommandId)
    {
      CurrentCommand = aCommandId;
      Running = true;

      if (OutputWindowConstants.commandName.ContainsKey(aCommandId))
      {
        OnClangCommandMessageTransfer(new ClangCommandMessageEventArgs($"\n{OutputWindowConstants.commandName[aCommandId].ToUpper()} STARTED... \n", true));
        StatusBarHandler.Status(OutputWindowConstants.commandName[aCommandId] + " started...", 1, vsStatusAnimation.vsStatusAnimationBuild, 1);
      }

      OnClangCommandBegin(new ClearErrorListEventArgs());
    }

    private void OnClangCommandBegin(ClearErrorListEventArgs e)
    {
      ClearErrorListEvent?.Invoke(this, e);
    }

    private void OnAfterClangCommand()
    {
      Running = false;
    }

    public void OnAfterStopCommand(object sender, CloseDataStreamingEventArgs e)
    {
      if (e.IsStopped)
      {
        OnClangCommandMessageTransfer(new ClangCommandMessageEventArgs($"\nCOMMAND STOPPED", false));
        StatusBarHandler.Status("Command stopped", 0, vsStatusAnimation.vsStatusAnimationBuild, 0);
      }
      else
      {
        OnClangCommandMessageTransfer(new ClangCommandMessageEventArgs($"\n{OutputWindowConstants.commandName[CurrentCommand].ToUpper()} FINISHED\n", false));
        StatusBarHandler.Status(OutputWindowConstants.commandName[CurrentCommand] + " finished", 0, vsStatusAnimation.vsStatusAnimationBuild, 0);
      }
    }


    private void OnClangCommandMessageTransfer(ClangCommandMessageEventArgs e)
    {
      ClangCommandMessageEvent?.Invoke(this, e);
    }


    public void OnFileHierarchyChanged(object sender, VsHierarchyDetectedEventArgs e)
    {
      HierarchyDetectedEvent?.Invoke(this, e);
    }


    public void OnMissingLLVMDetected(object sender, MissingLlvmEventArgs e)
    {
      MissingLlvmEvent?.Invoke(this, e);
    }


    private string GetCommandName(string aGuid, int aId)
    {
      try
      {
        if (null == aGuid)
          return string.Empty;

        if (null == mCommand)
          return string.Empty;

        Command cmd = mCommand.Item(aGuid, aId);
        if (null == cmd)
          return string.Empty;

        return cmd.Name;
      }
      catch (Exception) { }

      return string.Empty;
    }

    #endregion


    #region Events

    /// <summary>
    /// It is called before every command. Update the running state.  
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnBeforeClangCommand(object sender, EventArgs e)
    {
      UIUpdater.Invoke(() =>
      {
        if (!(sender is OleMenuCommand command))
          return;

        if (VsServiceProvider.TryGetService(typeof(DTE), out object dte) && !(dte as DTE2).Solution.IsOpen)
          command.Visible = command.Enabled = false;

        else if (VsBuildRunning && command.CommandID.ID != CommandIds.kSettingsId)
          command.Visible = command.Enabled = false;

        else
          command.Visible = command.Enabled = command.CommandID.ID != CommandIds.kStopClang ? !Running : Running;
      });
    }


    /// <summary>
    /// Set the VS running build flag to true when the VS build begin.
    /// </summary>
    /// <param name="Scope"></param>
    /// <param name="Action"></param>
    public void OnMSVCBuildBegin(vsBuildScope Scope, vsBuildAction Action) => VsBuildRunning = true;


    /// <summary>
    /// Set the VS running build flag to false when the VS build finished.
    /// </summary>
    /// <param name="Scope"></param>
    /// <param name="Action"></param>
    public async void OnMSVCBuildDone(vsBuildScope Scope, vsBuildAction Action)
    {
      VsBuildRunning = false;
      await OnMSVCBuildSucceededAsync();
    }


    private async System.Threading.Tasks.Task OnMSVCBuildSucceededAsync()
    {
      if (!CompileCommand.Instance.VsCompileFlag)
        return;

      var exitCode = int.MaxValue;
      if (VsServiceProvider.TryGetService(typeof(DTE), out object dte))
        exitCode = (dte as DTE2).Solution.SolutionBuild.LastBuildInfo;

      // VS compile detected errors and there is not necessary to run clang compile
      if (0 != exitCode)
      {
        CompileCommand.Instance.VsCompileFlag = false;
        return;
      }

      // Run clang compile after the VS compile succeeded

      OnBeforeClangCommand(CommandIds.kCompileId);
      await CompileCommand.Instance.RunClangCompileAsync(CommandIds.kCompileId, CommandUILocation.ContextMenu);
      CompileCommand.Instance.VsCompileFlag = false;
      OnAfterClangCommand();
    }


    public void OnBeforeSave(object sender, Document aDocument)
    {
      BeforeSaveClangTidy();
      BeforeSaveClangFormat(aDocument);
    }


    private void BeforeSaveClangTidy()
    {
      if (false == mSaveCommandWasGiven) // The save event was not triggered by Save File or SaveAll commands
        return;

      var tidyOption = SettingsProvider.TidySettings;

      if (false == tidyOption.AutoTidyOnSave) // The clang-tidy on save option is disable 
        return;

      if (true == Running) // Clang compile/tidy command is running
        return;

      TidyCommand.Instance.RunClangTidyAsync(CommandIds.kTidyFixId, CommandUILocation.ContextMenu);
      mSaveCommandWasGiven = false;
    }


    private void BeforeSaveClangFormat(Document aDocument)
    {
      var clangFormatOptionPage = SettingsProvider.ClangFormatSettings;
      var tidyOptionPage = SettingsProvider.TidySettings;

      if (CurrentCommand == CommandIds.kTidyFixId && Running && tidyOptionPage.FormatAfterTidy && clangFormatOptionPage.EnableFormatOnSave)
      {
        mDocument = aDocument;
        mFormatAfterTidyFlag = true;
        return;
      }

      if (false == clangFormatOptionPage.EnableFormatOnSave)
        return;

      if (false == Vsix.IsDocumentDirty(aDocument) && false == mFormatAfterTidyFlag)
        return;

      if (false == FileHasExtension(aDocument.FullName, clangFormatOptionPage.FileExtensions))
        return;

      if (true == SkipFile(aDocument.FullName, clangFormatOptionPage.FilesToIgnore))
        return;

      var option = SettingsProvider.ClangFormatSettings;
      ClangFormatCommand.Instance.FormatDocument(aDocument, option, CommandUILocation.Toolbar);
    }


    private bool SkipFile(string aFilePath, string aSkipFiles)
    {
      var skipFilesList = aSkipFiles.ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      return skipFilesList.Contains(Path.GetFileName(aFilePath).ToLower());
    }


    private bool FileHasExtension(string filePath, string fileExtensions)
    {
      var extensions = fileExtensions.ToLower().Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
      return extensions.Contains(Path.GetExtension(filePath).ToLower());
    }


    public void CommandEventsBeforeExecute(string aGuid, int aId, object aCustomIn, object aCustomOut, ref bool aCancelDefault)
    {
      BeforeExecuteClangCompile(aGuid, aId);
      BeforeExecuteClangTidy(aGuid, aId);
    }


    private void BeforeExecuteClangCompile(string aGuid, int aId)
    {
      var generalOptions = SettingsProvider.GeneralSettings;

      if (null == generalOptions || false == generalOptions.ClangCompileAfterVsCompile)
        return;

      string commandName = GetCommandName(aGuid, aId);
      if (0 != string.Compare("Build.Compile", commandName))
        return;

      CompileCommand.Instance.VsCompileFlag = true;
    }


    private void BeforeExecuteClangTidy(string aGuid, int aId)
    {
      string commandName = GetCommandName(aGuid, aId);
      if (0 != string.Compare("File.SaveAll", commandName) &&
        0 != string.Compare("File.SaveSelectedItems", commandName))
      {
        return;
      }
      mSaveCommandWasGiven = true;
    }

    #endregion
  }
}
