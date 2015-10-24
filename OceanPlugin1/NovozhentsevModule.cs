using System;
using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;

namespace NovozhentsevOceanPlugin
{
    /// <summary>
    /// This class will control the lifecycle of the Module.
    /// The order of the methods are the same as the calling order.
    /// </summary>
    public class NovozhentsevModule : IModule
    {
        public NovozhentsevModule()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        #region IModule Members

        /// <summary>
        /// This method runs once in the Module life; when it loaded into the petrel.
        /// This method called first.
        /// </summary>
        public void Initialize()
        {
            // TODO:  Add NovozhentsevModule.Initialize implementation
        }

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the not UI related components.
        /// (eg: datasource, plugin)
        /// </summary>
        public void Integrate()
        {            
            // TODO:  Add NovozhentsevModule.Integrate implementation
            
            // Register NovozhentsevLab2Workstep
            NovozhentsevLab2Workstep novozhentsevlab2workstepInstance = new NovozhentsevLab2Workstep();
            PetrelSystem.WorkflowEditor.AddUIFactory<NovozhentsevLab2Workstep.Arguments>(new NovozhentsevLab2Workstep.UIFactory());
            PetrelSystem.WorkflowEditor.Add(novozhentsevlab2workstepInstance, PetrelSystem.WorkflowEditor.RegisteredWorksteps.Processes.FindOrCreateWorkstepGroup("Novozhentsev Lab2"));
            PetrelSystem.ProcessDiagram.Add(new Slb.Ocean.Petrel.Workflow.WorkstepProcessWrapper(novozhentsevlab2workstepInstance), "Novozhentsev Lab2");
            
}

        /// <summary>
        /// This method runs once in the Module life. 
        /// In this method, you can do registrations of the UI related components.
        /// (eg: settingspages, treeextensions)
        /// </summary>
        public void IntegratePresentation()
        {

            // TODO:  Add NovozhentsevModule.IntegratePresentation implementation
        }

        /// <summary>
        /// This method called once in the life of the module; 
        /// right before the module is unloaded. 
        /// It is usually when the application is closing.
        /// </summary>
        public void Disintegrate()
        {
            // TODO:  Add NovozhentsevModule.Disintegrate implementation
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // TODO:  Add NovozhentsevModule.Dispose implementation
        }

        #endregion

    }


}