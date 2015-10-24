using System;

using Slb.Ocean.Core;
using Slb.Ocean.Petrel;
using Slb.Ocean.Petrel.UI;
using Slb.Ocean.Petrel.Workflow;
using Slb.Ocean.Petrel.DomainObject.Well;
using Slb.Ocean.Petrel.DomainObject.PillarGrid;
using System.Collections.Generic;
using Slb.Ocean.Geometry;
using Slb.Ocean.Petrel.DomainObject;
using Slb.Ocean.Basics;

namespace NovozhentsevOceanPlugin
{
    /// <summary>
    /// This class contains all the methods and subclasses of the NovozhentsevLab2Workstep.
    /// Worksteps are displayed in the workflow editor.
    /// </summary>
    class NovozhentsevLab2Workstep : Workstep<NovozhentsevLab2Workstep.Arguments>, IExecutorSource, IAppearance, IDescriptionSource
    {
        #region Overridden Workstep methods

        /// <summary>
        /// Creates an empty Argument instance
        /// </summary>
        /// <returns>New Argument instance.</returns>
        protected override NovozhentsevLab2Workstep.Arguments CreateArgumentPackageCore(IDataSourceManager dataSourceManager)
        {
            return new Arguments(dataSourceManager);
        }
        /// <summary>
        /// Copies the Arguments instance.
        /// </summary>
        /// <param name="fromArgumentPackage">the source Arguments instance</param>
        /// <param name="toArgumentPackage">the target Arguments instance</param>
        protected override void CopyArgumentPackageCore(Arguments fromArgumentPackage, Arguments toArgumentPackage)
        {
            DescribedArgumentsHelper.Copy(fromArgumentPackage, toArgumentPackage);
        }

        /// <summary>
        /// Gets the unique identifier for this Workstep.
        /// </summary>
        protected override string UniqueIdCore
        {
            get
            {
                return "06bb3c5e-d650-4321-916e-d09814d87a39";
            }
        }
        #endregion

        #region IExecutorSource Members and Executor class

        /// <summary>
        /// Creates the Executor instance for this workstep. This class will do the work of the Workstep.
        /// </summary>
        /// <param name="argumentPackage">the argumentpackage to pass to the Executor</param>
        /// <param name="workflowRuntimeContext">the context to pass to the Executor</param>
        /// <returns>The Executor instance.</returns>
        public Slb.Ocean.Petrel.Workflow.Executor GetExecutor(object argumentPackage, WorkflowRuntimeContext workflowRuntimeContext)
        {
            return new Executor(argumentPackage as Arguments, workflowRuntimeContext);
        }

        public class Executor : Slb.Ocean.Petrel.Workflow.Executor
        {
            Arguments arguments;
            WorkflowRuntimeContext context;

            public Executor(Arguments arguments, WorkflowRuntimeContext context)
            {
                this.arguments = arguments;
                this.context = context;
            }

            public override void ExecuteSimple()
            {
               /* PetrelLogger.InfoOutputWindow(string.Format("Well Log: {0},Grid: {1}",
                    arguments.NovozhentsevWellLog.Name,
                    arguments.NovozhentsevGrid.Name));
                PetrelLogger.InfoOutputWindow("Runtime contex: "+ context.GetType().FullName
                    );*/

                Property prop = null;
                int count = 0;
                // все действия по изменению данных строго внутри транзакции
                using (ITransaction trans = DataManager.NewTransaction())//объект удалением которого будет  с#
                {
                    // запрашиваем исключительный доступ к свойствам сетки
                    trans.Lock(arguments.NovozhentsevGrid.PropertyCollection);//подождать пока
                    // создаем новое свойство и задаем его имя
                    prop = arguments.NovozhentsevGrid.PropertyCollection.CreateProperty(arguments.NovozhentsevWellLog.WellLogVersion.Template);
                    prop.Name = string.Format("{0} ({1}) upscaled", arguments.NovozhentsevWellLog.Name, arguments.NovozhentsevWellLog.Borehole.Description.Name);
                    arguments.NovozhentsevResultProperty = prop;
                    // получаем перечислитель замеров каротажки в явном виде
                    IEnumerator<WellLogSample> enumSamples = arguments.NovozhentsevWellLog.Samples.GetEnumerator();//<> generic type тип элемента коллекции нумератор которой имеет этот тип,итератор
                    // получаем доступ к штатному сервису Petrel:
                    IPillarGridIntersectionService pgIntersection = CoreSystem.GetService<IPillarGridIntersectionService>();//статический метод пиллар- пилоны -вид сетки
                    // получаем траекторию скважины в виде линии и запрашиваем список пересечений этой линии с ячейками сетки
                    IPolyline3 polyline = arguments.NovozhentsevWellLog.Borehole.Trajectory.Polyline;//
                    IEnumerable<SegmentCellIntersection> intersectionsegments = pgIntersection.GetPillarGridPolylineIntersections(arguments.NovozhentsevGrid, polyline);//набор пересечений полилайна с сеткой грида/коллекция
                    // проходим в цикле по найденным точкам пересечения
                    SegmentCellIntersection enteringSegment = new SegmentCellIntersection();
                    SegmentCellIntersection leavingSegment;
                    double enteringMD = double.NaN;//measure depth, true vertical depth
                    double leavingMD = double.NaN;
                    bool FirstTime = true;
                    foreach (SegmentCellIntersection segment in intersectionsegments)
                    {
                        // на первой итерации нужно только инициализировать внутренние переменные
                        if (FirstTime)
                        {
                            FirstTime = false;
                            enteringSegment = segment;
                            enteringMD = arguments.NovozhentsevWellLog.Borehole.Transform(arguments.NovozhentsevGrid.Domain, segment.IntersectionPoint.Z, Domain.MD);//домен - система координат, отсчета
                            // проматываем цикл по замерам, пока не дойдем до нужной глубины
                            while (enumSamples.MoveNext())
                            {
                                if (enumSamples.Current.MD >= enteringMD)
                                    break;
                            }
                            continue;
                        }
                        leavingSegment = segment;
                        // находим измеренные глубины точек пересечения вдоль ствола скважины
                        enteringMD = arguments.NovozhentsevWellLog.Borehole.Transform(arguments.NovozhentsevGrid.Domain, enteringSegment.IntersectionPoint.Z, Domain.MD);
                        leavingMD = arguments.NovozhentsevWellLog.Borehole.Transform(arguments.NovozhentsevGrid.Domain, leavingSegment.IntersectionPoint.Z, Domain.MD);
                        // находим индекс ячейки
                        Index3 cellIndex = enteringSegment.EnteringCell;
                        float avg = float.NaN;
                        // если текущее значение глубины замера внутри текущей ячейки
                        if (enumSamples.Current.MD <= leavingMD)
                        {
                            int numSamples = 1;
                            float total = enumSamples.Current.Value;
                            // вручную проматываем замеры каротажной кривой в этой ячейке
                            while (enumSamples.MoveNext())
                            {
                                if (enumSamples.Current.MD <= leavingMD)
                                {
                                    numSamples++;
                                    total += enumSamples.Current.Value;
                                }
                                else
                                    break;
                            }
                            // теперь вычисляем усредненное значение свойства
                            avg = (float)(total / numSamples);
                            PetrelLogger.InfoOutputWindow(string.Format("сell= {0}, value={1} ({2})", cellIndex.ToString(), avg, prop.Description.Name));
                            prop[cellIndex] = avg;
                            count++;
                            enteringSegment = leavingSegment;
                        }
                        // и записываем его в новое свойство сетки

                    }
                    // если проблем нет, следующая строчка создаст наше свойство
                    arguments.NovozhentsevNumCells = count;
                    trans.Commit();
                }


                // TODO: Implement the workstep logic here.
            }
        }

        #endregion

        /// <summary>
        /// ArgumentPackage class for NovozhentsevLab2Workstep.
        /// Each public property is an argument in the package.  The name, type and
        /// input/output role are taken from the property and modified by any
        /// attributes applied.
        /// </summary>
        public class Arguments : DescribedArgumentsByReflection
        {
            public Arguments()
                : this(DataManager.DataSourceManager)
            {                
            }

            public Arguments(IDataSourceManager dataSourceManager)
            {
            }

            private Slb.Ocean.Petrel.DomainObject.Well.WellLog novozhentsevWellLog = WellLog.NullObject;
            private Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid novozhentsevGrid = Grid.NullObject;
            private int novozhentsevNumCells;
            private Slb.Ocean.Petrel.DomainObject.PillarGrid.Property novozhentsevResultProperty;

            [Description("Well Log", "Каротажная кривая")]
            public Slb.Ocean.Petrel.DomainObject.Well.WellLog NovozhentsevWellLog
            {
                internal get { return this.novozhentsevWellLog; }//геттер только класс
                set { this.novozhentsevWellLog = value; }//сетте, кто хочет
            }

            [Description("Grid", "3D-сетка")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Grid NovozhentsevGrid
            {
                internal get { return this.novozhentsevGrid; }
                set { this.novozhentsevGrid = value; }
            }

            [Description("Num Cells", "Количество ячеек")]
            public int NovozhentsevNumCells
            {
                get { return this.novozhentsevNumCells; }
                internal set { this.novozhentsevNumCells = value; }
            }

            [Description("Property", "Результирующее свойство сетки")]
            public Slb.Ocean.Petrel.DomainObject.PillarGrid.Property NovozhentsevResultProperty
            {
                get { return this.novozhentsevResultProperty; }
                internal set { this.novozhentsevResultProperty = value; }
            }


        }
    
        #region IAppearance Members
        public event EventHandler<TextChangedEventArgs> TextChanged;
        protected void RaiseTextChanged()
        {
            if (this.TextChanged != null)
                this.TextChanged(this, new TextChangedEventArgs(this));
        }

        public string Text
        {
            get { return Description.Name; }
            private set 
            {
                // TODO: implement set
                this.RaiseTextChanged();
            }
        }

        public event EventHandler<ImageChangedEventArgs> ImageChanged;
        protected void RaiseImageChanged()
        {
            if (this.ImageChanged != null)
                this.ImageChanged(this, new ImageChangedEventArgs(this));
        }

        public System.Drawing.Bitmap Image
        {
            get { return PetrelImages.Modules; }
            private set 
            {
                // TODO: implement set
                this.RaiseImageChanged();
            }
        }
        #endregion

        #region IDescriptionSource Members

        /// <summary>
        /// Gets the description of the NovozhentsevLab2Workstep
        /// </summary>
        public IDescription Description
        {
            get { return NovozhentsevLab2WorkstepDescription.Instance; }
        }

        /// <summary>
        /// This singleton class contains the description of the NovozhentsevLab2Workstep.
        /// Contains Name, Shorter description and detailed description.
        /// </summary>
        public class NovozhentsevLab2WorkstepDescription : IDescription
        {
            /// <summary>
            /// Contains the singleton instance.
            /// </summary>
            private  static NovozhentsevLab2WorkstepDescription instance = new NovozhentsevLab2WorkstepDescription();
            /// <summary>
            /// Gets the singleton instance of this Description class
            /// </summary>
            public static NovozhentsevLab2WorkstepDescription Instance
            {
                get { return instance; }
            }

            #region IDescription Members

            /// <summary>
            /// Gets the name of NovozhentsevLab2Workstep
            /// </summary>
            public string Name
            {
                get { return "Lab2 workstep (Novozhentsev)"; }
            }
            /// <summary>
            /// Gets the short description of NovozhentsevLab2Workstep
            /// </summary>
            public string ShortDescription
            {
                get { return "Алгоритм расчёта средних значений каротажной кривой в ячейках 3D-сетки"; }
            }
            /// <summary>
            /// Gets the detailed description of NovozhentsevLab2Workstep
            /// </summary>
            public string Description
            {
                get { return "Лабораторная работа №2. Автор Новоженцев Вадим (АС-11-05) Через город лежала прямая дорога на Москву. На него напали с двух сторон. Тяжелейший бой шел почти неделю. Но преодолеть этот рубеж гитлеровцы так и не смогли."; }
            }

            #endregion
        }
        #endregion

        public class UIFactory : WorkflowEditorUIFactory
        {
            /// <summary>
            /// This method creates the dialog UI for the given workstep, arguments
            /// and context.
            /// </summary>
            /// <param name="workstep">the workstep instance</param>
            /// <param name="argumentPackage">the arguments to pass to the UI</param>
            /// <param name="context">the underlying context in which the UI is being used</param>
            /// <returns>a Windows.Forms.Control to edit the argument package with</returns>
            protected override System.Windows.Forms.Control CreateDialogUICore(Workstep workstep, object argumentPackage, WorkflowContext context)//cоздать ядро диалогового ядра, все компоненты от контролл
            {
                return new NovozhentsevLab2WorkstepUI((NovozhentsevLab2Workstep)workstep, (Arguments)argumentPackage, context);
            }
        }

    }
}