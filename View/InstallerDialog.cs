using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RMA.RhiExec.Model;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.View
{
  partial class InstallerDialog : Form
  {
    public InstallerDialog()
    {
      InitializeComponent();
      this.StartPosition = FormStartPosition.CenterScreen;
      m_Panel = new RMA.RhiExec.View.PanelInitializing();
    }

    private void InstallerDialog_Shown(object sender, EventArgs e)
    {
      try
      {
        InstallerEngine.StartEngine(this);
      }
      catch (Exception ex)
      {
        ShowErrorDialog(ex);
        Logger.Log(LogLevel.Error, ex);
      }
    }

    public void ShowInitializationDialog()
    {
      if (Program.m_options.SilentInstall)
        return;
      else
      {
        DebugLog("ShowInitializationDialog starting");
        PanelInitializing panel = new PanelInitializing();
        ShowPanel(panel);
        DebugLog("ShowInitializationDialog ending");
      }
    }

    public void ShowErrorDialog()
    {
      ShowErrorDialog("An unexpected error has occurred during installation.");
    }

    public void ShowErrorDialog(string message)
    {
      Logger.Log(LogLevel.Error, message);

      if (Program.m_options.SilentInstall)
        InstallerEngine.EndInstallation();
      else
      {
        DebugLog("ShowErrorDialog starting");
        PanelMessage panel = new PanelMessage();
        panel.SetMessage(message);
        ShowPanel(panel);
        DebugLog("ShowErrorDialog ending");
      }
    }

    public void ShowErrorDialog(Exception ex)
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("Exception: " + ex.GetType()).Append("\n");
      sb.Append("Message: " + ex.Message).Append("\n");
      sb.Append("Source: " + ex.Source).Append("\n");
      sb.Append("StackTrace: " + ex.StackTrace).Append("\n");

      Exception inner = ex.InnerException;
      while (inner != null)
      {
        sb.Append("\nException: " + ex.GetType()).Append("\n");
        sb.Append("Inner Exception: " + ex.Message).Append("\n");
        sb.Append("Source: " + ex.Source).Append("\n");
        sb.Append("StackTrace: " + ex.StackTrace).Append("\n");

        inner = inner.InnerException;
      }

      ShowErrorDialog(sb.ToString());
    }

    public void ShowWelcomeDialog()
    {
      if (Program.m_options.SilentInstall)
        InstallerEngine.InstallAsync();
      else
      {
        DebugLog("ShowWelcomeDialog starting");
        PanelWelcome panel = new PanelWelcome();
        panel.SetTitle(InstallerEngine.PackageTitle);
        ShowPanel(panel);
        DebugLog("ShowWelcomeDialog ending");
      }
    }

    public void ShowProgressDialog()
    {
      if (Program.m_options.SilentInstall)
        return;

      DebugLog("ShowProgressDialog starting");
      PanelInstalling panel = new PanelInstalling();
      ShowPanel(panel);
      DebugLog("ShowProgressDialog ending");
    }

    public void SetPercentComplete(int percentComplete)
    {
      if (Program.m_options.SilentInstall)
        return;

      PanelInstalling currentPanel = GetCurrentPanel() as PanelInstalling;
      if (currentPanel == null)
        return;

      currentPanel.setProgress(percentComplete);
    }

    public void ShowCompleteDialog()
    {
      if (Program.m_options.SilentInstall)
        InstallerEngine.EndInstallation();
      else
      {
        DebugLog("ShowCompleteDialog starting");
        PanelComplete panel = new PanelComplete();
        ShowPanel(panel);
        DebugLog("ShowCompleteDialog ending");
      }
    }

    public void ShowAlreadyInstalledDialog()
    {
      if (Program.m_options.SilentInstall)
        InstallerEngine.EndInstallation();
      else
      {
        DebugLog("ShowAlreadyInstalledDialog starting");
        PanelMessage panel = new PanelMessage();
        panel.SetMessage("This package is already installed.");
        ShowPanel(panel);
        DebugLog("ShowAlreadyInstalledDialog ending");
      }
    }


    private void btnNext_Click(object sender, EventArgs e)
    {
      DebugLog("Next Button Clicked");
      if (InstallerEngine.CurrentPhase() == InstallerPhase.Initialized)
        InstallerEngine.InstallAsync();
      else
        InstallerEngine.EndInstallation();
    }
    private void btnCancel_Click(object sender, EventArgs e)
    {
      DebugLog("Cancel Button Clicked");
      InstallerEngine.CancelInstallation();
    }


    public void ShowPanel(UserControl panel)
    {
      DebugLog("ShowPanel starting");
      UserControl current = GetCurrentPanel();
      if (current != panel)
        SwapPanel(current, panel);
      DebugLog("ShowPanel ending");
    }

    private void SwapPanel(UserControl existingPanel, UserControl newPanel)
    {
      DebugLog("SwapPanel starting");
      Point loc = new Point();
      Size size = new Size();

      loc = existingPanel.Location;
      size = existingPanel.Size;
      Controls.Remove(existingPanel);

      newPanel.Location = loc;
      newPanel.Size = size;
      Controls.Add(newPanel);

      SetButtonState(newPanel);
      DebugLog("SwapPanel ending");
    }

    #region Button State Management
    private void SetButtonState(UserControl panel)
    {
      AllButtonStates state = new AllButtonStates();
      if (panel is PanelComplete)
      {
        state.Close = ButtonState.Enabled;
        state.Back = ButtonState.Hidden;
        state.Next = ButtonState.Hidden;
      }
      else if (panel is PanelInitializing)
      {
        state.Cancel = ButtonState.Enabled;
        state.Next = ButtonState.Disabled;
        state.Back = ButtonState.Hidden;
      }
      else if (panel is PanelInstalling)
      {
        state.Cancel = ButtonState.Enabled;
        state.Next = ButtonState.Disabled;
        state.Back = ButtonState.Hidden;
      }
      else if (panel is PanelWelcome)
      {
        state.Cancel = ButtonState.Enabled;
        state.Next = ButtonState.Enabled;
        state.Back = ButtonState.Hidden;
      }
      else if (panel is PanelMessage)
      {
        state.Cancel = ButtonState.Hidden;
        state.Close = ButtonState.Enabled;
        state.Next = ButtonState.Hidden;
        state.Back = ButtonState.Hidden;
      }

      SetButtonState(state);
    }

    public void SetButtonState(AllButtonStates state)
    {
      btnNext.Text = Rhino.UI.Localization.LocalizeString("Next >", 1);
      SetButtonState(btnBack, state.Back);
      SetButtonState(btnNext, state.Next);
      SetButtonState(btnCancel, state.Cancel);
      if (state.Close == ButtonState.Disabled || state.Close == ButtonState.Enabled)
      {
        btnNext.Text = Rhino.UI.Localization.LocalizeString("Close", 2);
        SetButtonState(btnNext, state.Close);
      }
    }

    private static void SetButtonState(Button btn, ButtonState state)
    {
      switch (state)
      {
        case (ButtonState.Disabled):
          btn.Visible = true;
          btn.Enabled = false;
          break;
        case (ButtonState.Hidden):
          btn.Visible = false;
          break;
        case (ButtonState.Enabled):
          btn.Visible = true;
          btn.Enabled = true;
          break;
      }
    }
    #endregion

    private UserControl GetCurrentPanel()
    {
      foreach (Control c in Controls)
      {
        UserControl uc = c as UserControl;
        if (uc != null)
        {
          return uc;
        }
      }
      return null;
    }

    private void InstallerDialog_Load(object sender, EventArgs e)
    {
      DebugLog("InstallerDialog_Load starting");
      SetButtonState(GetCurrentPanel());
      Rhino.UI.Localization.LocalizeForm(this);
      DebugLog("InstallerDialog_Load ending");
    }

    private void InstallerDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (InstallerEngine.CurrentPhase() == InstallerPhase.Complete || InstallerEngine.CurrentPhase() == InstallerPhase.Success)
      {
        // Do nothing; everything finished successfully.
      }
      else
      {
        DebugLog("InstallerDialog_FormClosing starting");
        InstallerEngine.CancelInstallation();
        DebugLog("InstallerDialog_FormClosing ending");
      }
    }

    private static void DebugLog(string msg)
    {
      InstallerEngine.DebugLog(msg);
    }

    protected override bool ShowWithoutActivation
    {
      get
      {
        return true;
      }
    }
  }

  public enum UIButtonEvent
  {
    Unknown,
    Back,
    Next,
    Cancel,
    Close,
  }

  public enum ButtonState
  {
    Hidden = 0,
    Enabled = 1,
    Disabled = 2,
  }

  class AllButtonStates
  {
    public ButtonState Back = ButtonState.Hidden;
    public ButtonState Next = ButtonState.Hidden;
    public ButtonState Cancel = ButtonState.Hidden;
    public ButtonState Close = ButtonState.Hidden;
  }

}