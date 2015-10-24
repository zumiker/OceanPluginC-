using System;
using System.Drawing;
using System.Windows.Forms;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using Slb.Ocean.Petrel.DomainObject.Well;
using Slb.Ocean.Petrel.UI;
//using Slb.Ocean.Petrel.UI.Visualization3D;
using Slb.Ocean.Petrel.Workflow;

namespace NovozhentsevOceanPlugin
{
    /// <summary>
    /// This class is the user interface which forms the focus for the capabilities offered by the process.  
    /// This often includes UI to set up arguments and interactively run a batch part expressed as a workstep.
    /// </summary>
    partial class NovozhentsevLab2WorkstepUI : UserControl
    {
        private NovozhentsevLab2Workstep workstep;
        private bool k=false,ka=false;
        /// <summary>
        /// The argument package instance being edited by the UI.
        /// </summary>
        private NovozhentsevLab2Workstep.Arguments args;
        /// <summary>
        /// Contains the actual underlaying context.
        /// </summary>
        private WorkflowContext context;
        private NovozhentsevLab2Workstep.Arguments tmpargs = new NovozhentsevLab2Workstep.Arguments();
        /// <summary>
        /// Initializes a new instance of the <see cref="NovozhentsevLab2WorkstepUI"/> class.
        /// </summary>
        /// <param name="workstep">the workstep instance</param>
        /// <param name="args">the arguments</param>
        /// <param name="context">the underlying context in which this UI is being used</param>
        public NovozhentsevLab2WorkstepUI(NovozhentsevLab2Workstep workstep, NovozhentsevLab2Workstep.Arguments args, WorkflowContext context)
        {
            InitializeComponent();

            this.workstep = workstep;
            this.args = args;
            this.context = context;

            workstep.CopyArgumentPackage(args, tmpargs);
            lblShortDesc.Text = workstep.Description.ShortDescription;
            lblLongDesc.Text = workstep.Description.Description;
            btnApply.Image = PetrelImages.Apply;
            btnOK.Image = PetrelImages.OK;
            btnCancel.Image = PetrelImages.Cancel;
        }

        private void tgtWellLog_DragDrop(object sender, DragEventArgs e)
        {
            WellLog welllog = e.Data.GetData(typeof(object)) as WellLog;
            if (welllog == null) {
                PetrelLogger.ErrorBox("Объект не является каротажной кривой!");
                return;
            }
            tmpargs.NovozhentsevWellLog = welllog;
            UpdateUIFormArgs();
        }

        private void tgtGrid_DragDrop(object sender, DragEventArgs e)
        {
            Grid grid = e.Data.GetData(typeof(object)) as Grid;
            if (grid == null)
            {
                PetrelLogger.ErrorBox("Объект не является гридом!");
                return;
            }
            tmpargs.NovozhentsevGrid = grid;
            UpdateUIFormArgs();
        }

        private void UpdateUIFormArgs() {
            boxWellLog.Text = "";
            boxWellLog.Image = null;
            boxGrid.Text = "";
            boxGrid.Image = null;
            btnApply.Enabled = false;
            btnOK.Enabled = false;
            btnShow.Enabled = false;
            ka = false;
            k = false;
            boxProp.Text = "";
            boxProp.Image = null;
            lblNumCells.Text = "";
            if (tmpargs.NovozhentsevWellLog != null)
            {
                INameInfoFactory nameSvc = CoreSystem.GetService<INameInfoFactory>(tmpargs.NovozhentsevWellLog);
                boxWellLog.Text = string.Format("{0} ({1})", nameSvc.GetNameInfo(tmpargs.NovozhentsevWellLog).Name, tmpargs.NovozhentsevWellLog.Borehole.Description.Name);
                IImageInfoFactory imgSvc = CoreSystem.GetService<IImageInfoFactory>(tmpargs.NovozhentsevWellLog.Borehole);
                boxWellLog.Image = imgSvc.GetImageInfo(tmpargs.NovozhentsevWellLog).TypeImage;

            }
            if (tmpargs.NovozhentsevGrid != null)
            {
                INameInfoFactory nameSvc = CoreSystem.GetService<INameInfoFactory>(tmpargs.NovozhentsevGrid);
                boxGrid.Text = string.Format("{0} - {1}", nameSvc.GetNameInfo(tmpargs.NovozhentsevGrid).Name, Math.Round(tmpargs.NovozhentsevGrid.BoundingBox.Width*tmpargs.NovozhentsevGrid.BoundingBox.Length));
                IImageInfoFactory imgSvc = CoreSystem.GetService<IImageInfoFactory>(tmpargs.NovozhentsevGrid);
                boxGrid.Image = imgSvc.GetImageInfo(tmpargs.NovozhentsevGrid).TypeImage;

            }
            if (tmpargs.NovozhentsevWellLog != null && tmpargs.NovozhentsevGrid != null)
            {
                btnApply.Enabled = true;
                btnOK.Enabled = true;
            }
            if (tmpargs.NovozhentsevResultProperty != null)
            {
                btnShow.Enabled = true;
                boxProp.Text = tmpargs.NovozhentsevResultProperty.Description.Name;
                IImageInfoFactory imgSvc = CoreSystem.GetService<IImageInfoFactory>(tmpargs.NovozhentsevResultProperty);
                boxProp.Image = imgSvc.GetImageInfo(tmpargs.NovozhentsevResultProperty).TypeImage;
            }
            if (tmpargs.NovozhentsevNumCells != 0)
            {
                lblNumCells.Text = tmpargs.NovozhentsevNumCells.ToString();
                lblNumCells.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        private void NovozhentsevLab2WorkstepUI_Load(object sender, EventArgs e)
        {
            UpdateUIFormArgs();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (context is WorkstepProcessWrapper.Context) {
                Executor exec = workstep.GetExecutor(tmpargs, new WorkstepProcessWrapper.RuntimeContext());
                exec.ExecuteSimple();
                UpdateUIFormArgs();
            }
            workstep.CopyArgumentPackage(tmpargs, args);
            k = true;
            context.OnArgumentPackageChanged(this, new WorkflowContext.ArgumentPackageChangedEventArgs());
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if(!k)
                btnApply_Click(sender, e);
            this.ParentForm.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if(ka)
                PetrelProject.ToggleWindows.Remove(PetrelProject.ToggleWindows.Active);
            tmpargs.NovozhentsevGrid = null;
            tmpargs.NovozhentsevNumCells = 0;
            tmpargs.NovozhentsevResultProperty = null;
            tmpargs.NovozhentsevWellLog = null;
            UpdateUIFormArgs();
            workstep.CopyArgumentPackage(tmpargs, args);
            this.ParentForm.Close();
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            Window3D win3d = PetrelProject.ToggleWindows.Add(WellKnownWindows.Window3D) as Window3D;
            win3d.ShowObject(args.NovozhentsevWellLog.Borehole);
            win3d.ShowObject(args.NovozhentsevResultProperty);
            win3d.ShowAxis = true;
            win3d.ShowAutoLegend = true;
            win3d.ZScale = 5;
            ka = true;
        }
    }
}
