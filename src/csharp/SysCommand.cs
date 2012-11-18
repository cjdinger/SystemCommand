// ---------------------------------------------------------------
// Copyright 2008, SAS Institute Inc.
// ---------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Xml;		    // for XMLTextWriter and XMLTextReader
using System.IO;		    // for StreamReader and StreamWriter
using SAS.Shared.AddIns;	// for access to the Add-in interfaces
using SAS.Tasks.Toolkit;    // for access to the SAS Task Toolkit

namespace SAS.Tasks.Examples.SysCommand
{
	/// <summary>
	/// This task processes system commands as if in a Windows batch file.
    /// It can be a handy way to get around the XCMD limitation 
    /// encountered in most SAS server deployments.
    /// 
    /// This task uses the SAS.Shared.AddIns.ISASTaskExecution interface,
    /// which tells SAS Enterprise Guide that the task will "run itself".
    /// The Run method performs the work and writes information to the log
    /// so that it can be stored in the project.
	/// </summary>
    [ClassId("E3B71B12-9930-45de-B396-ACF8661E0F48")]
    [Version(4.2)]
    [InputRequired(InputResourceType.None)]
    // IconLocation is the namespace-qualified name of the icon
    // it is built into this assembly as an Embedded Resource.
    [IconLocation("SAS.Tasks.Examples.SysCommand.SysCommand.ico")]
	public class SysCommand : SAS.Tasks.Toolkit.SasTask, SAS.Shared.AddIns.ISASTaskExecution
    {
        #region Initialization
        private string cmds = "REM Add commands to execute";

        public SysCommand()
		{
            InitializeComponent();
		}

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SysCommand));
            // 
            // SysCommand
            // 
            this.GeneratesReportOutput = false;
            this.GeneratesSasCode = false;
            this.RequiresData = false;
            resources.ApplyResources(this, "$this");

        }
        #endregion

        #region Save/restore state
        public override void RestoreStateFromXml(string xmlState)
        {
            if (xmlState != null && xmlState.Length > 0)
            {
                try
                {
                    using (StringReader sr = new StringReader(xmlState))
                    {
                        XmlTextReader reader = new XmlTextReader(sr);
                        reader.ReadStartElement("SysCommands");
                        cmds = reader.ReadElementString("Commands");
                        reader.ReadEndElement();
                        reader.Close();
                    }
                }
                catch
                {
                }
            }			
        }

        public override string GetXmlState()
        {
            using (StringWriter sw = new StringWriter())
            {
                XmlTextWriter writer = new XmlTextWriter(sw);
                writer.WriteStartElement("SysCommands");
                writer.WriteElementString("Commands", cmds);
                writer.WriteEndElement();
                writer.Close();
                return sw.ToString();
            }
        }
        #endregion

        #region Show the task form
        /// <summary>
        /// Show the form that allows the user to enter commands
        /// </summary>
        /// <param name="Owner"></param>
        /// <returns></returns>
        override public ShowResult Show(IWin32Window Owner)
		{
			SysCommandForm dlg = new SysCommandForm();
            dlg.Consumer = this.Consumer;
            // populate the form with the saved commands, if any
			dlg.Cmds = cmds;
			dlg.Text = this.Label;
			if (dlg.ShowDialog(Owner) == DialogResult.OK)
			{
                // get the updated commands
				cmds = dlg.Cmds;
				return ShowResult.RunNow;
			}
			else
				return ShowResult.Canceled;
        }
        #endregion

        #region Execute commands in a batch file
        /// <summary>
        /// This method writes out the system commands to 
        /// a CMD file to run as a batch process.
        /// It then defines and starts a process to
        /// run the CMD file.
        /// 
        /// File operations and Process operations can
        /// throw exceptions, so be sure to wrap the call
        /// to this routine within a try/catch if 
        /// you want to handle the errors.
        /// </summary>
        /// <returns>a log with the stdout content from the batch job</returns>
        public string ExecuteCommands()
		{
			string stdout;

            // create a temp batch file to run the commands
			string fn = Path.GetTempFileName();
			File.Move(fn, fn+".cmd");
			fn += ".cmd";

			using (StreamWriter sw = new StreamWriter(fn))
			{
				sw.Write(cmds);
			}

			// launch a process to run the batch file
			// record the stdout so we can report in the log for the task
			System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
			psi.FileName = fn;
			psi.RedirectStandardOutput = true;
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;

			System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
			stdout = p.StandardOutput.ReadToEnd();

			p.WaitForExit();

			// cleanup the temp file
			File.Delete(fn);

			// return the stdout
			return stdout;			
		}
		#endregion

        #region ISASTaskExecution Members

        /// <summary>
        /// We do not support Cancel for this task, so return false.
        /// </summary>
        /// <returns></returns>
        public bool Cancel()
        {
            return false;
        }

        /// <summary>
        /// This task returns nothing but a log, so we don't
        /// expect this method to be called
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public ISASTaskStream OpenResultStream(int Index)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// No results returned except for the log.
        /// </summary>
        public int ResultCount
        {
            get { return 0; }
        }

        /// <summary>
        /// Note: In the Run method it's a good idea to put *anything*
        /// that might throw an exception inside a try-catch block.
        /// And do not throw/rethrow any exceptions!
        /// 
        /// The host application might run this routine on a different
        /// thread other than the main application thread, and 
        /// it might not be able to handle an exception thrown
        /// within here.
        /// 
        /// Best practice: catch any potential exceptions and then
        /// write the appropriate information to the log using the 
        /// SAS.Tasks.Toolkit.Helpers.FormattedLogWriter class.  If you
        /// encounter an error, return RunStatus.Error.
        /// </summary>
        /// <param name="LogWriter"></param>
        /// <returns></returns>
        public RunStatus Run(ISASTaskTextWriter LogWriter)
        {
            RunStatus rc = RunStatus.Success;

            // to keep track of elapsed time for the system commands
            DateTime start = DateTime.Now;

            // seed the machine name for use in the log
            // We need to make it clear that the system commands
            // are executed on the local machine, not on a
            // remote SAS server machine.
            string machineName = "local machine";
            try
            {
                // Environment.MachineName can throw an InvalidOperationException
                machineName = Environment.MachineName;
            }
            catch (InvalidOperationException)
            {
                // couldn't get the machine name
            }

            // the "FormattedLogWriter" helps color-code your log output
            // for NOTE, ERROR, WARNING lines.
            SAS.Tasks.Toolkit.Helpers.FormattedLogWriter.WriteNoteLine(LogWriter,
                string.Format("NOTE: Running system commands on {0}. \nOutput:", machineName));
            try
            {
                string log = ExecuteCommands();

                // write the output collected from stdout
                SAS.Tasks.Toolkit.Helpers.FormattedLogWriter.WriteNormalLine(LogWriter, log);
            }
            catch (Exception ex)
            {
                // if there is an error, place it in the log
                SAS.Tasks.Toolkit.Helpers.FormattedLogWriter.WriteErrorLine(LogWriter,
                    string.Format("ERROR: Could not run commands \n{0}",ex.Message));

                // return error status so that it gets the "red X" treatment
                rc = RunStatus.Error;
            }

            TimeSpan elapsedTime = TimeSpan.FromTicks(DateTime.Now.Ticks - start.Ticks);

            SAS.Tasks.Toolkit.Helpers.FormattedLogWriter.WriteNoteLine(LogWriter, 
                string.Format("NOTE: System commands completed. \n\tReal time: {0:F2} seconds", elapsedTime.TotalSeconds));

            return rc;
        }

        #endregion
    }
}
