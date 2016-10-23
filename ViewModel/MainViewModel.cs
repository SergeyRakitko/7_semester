using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Data.Entity;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using Vector = VisualisationLib.Vector;
using VisualisationLib;

namespace ViewModel
{
    public class DataBaseClimateModel
    {
        // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        [Column("Название модели")]
        public string Description { get; set; }
        [Column("Дата создания")]
        public DateTime Date { get; set; }
        [Column("Число пробных точек")]
        public int M { get; set; }
        [Column("Время отслеживания частицы")]
        public int T { get; set; }
        [Column("Число точек в следе")]
        public int K { get; set; }
        [Column("Массив следов")]
        public byte[] Blob { get; set; }
    }

    public class ClimateContext : DbContext
    {
        public ClimateContext() : base("ClimaticData2") { }

        public DbSet<DataBaseClimateModel> DataBaseClimateModels { get; set; }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        // climate model parameters
        private int _lonPoints, _latPoints, _latMin, _latMax, _lonMin, _lonMax;
        private Vector[,] _vectorField;
        // database context
        private readonly ClimateContext _context;

        public MainViewModel()
        {
            Mistake = "";
            Search = "";
            IsEnabled = true;
            ImagePath = @"~\..\images\world.jpg"; // set image path
            ReadFile("../../../Data/d1-3-velocity.txt"); // read climate model parameters
            
            // set sizes of an image
            ImageWidth = 1000;
            ImageHeight = 500;
            
            _climateModelCollection = new ObservableCollection<ClimateModel>();
            _filterClimateModelCollection = new ObservableCollection<ClimateModel>();

            _context = new ClimateContext();

            // initialization
            //for (int i = 0; i < 500; i++)
            //{
            //    _inputClimateModel = new ClimateModel();
            //    _inputClimateModel.ModelData = new ParticleTrack(_vectorField, 11 + i, 5 + i / 100, 50 + i / 10, _lonPoints, _latPoints, _lonMax - _lonMin, _latMax - _latMin);
            //    _inputClimateModel.ModelData.ParallelTracksComputation();
            //    _inputClimateModel.ModelDescription = "model" + (i + 1);
            //    _inputClimateModel.ModelDate = new DateTime(1900 + i / 5, 1 + i / 50, 1 + i / 20);

            //    addClimateModel(null);
            //}

            // retrieve data from the database
            var query = from dbmodel in _context.DataBaseClimateModels select dbmodel;
            foreach (var dbmodel in query)
            {
                ClimateModel model = new ClimateModel
                {
                    ModelData = new ParticleTrack(_vectorField, dbmodel.M, dbmodel.T, dbmodel.K, _lonPoints,
                        _latPoints, _lonMax - _lonMin,
                        _latMax - _latMin) {ParallelTracks = ReadBlob(dbmodel.Blob)},
                    Id = dbmodel.Id,
                    ModelDate = dbmodel.Date,
                    ModelDescription = dbmodel.Description
                };

                _climateModelCollection.Add(model);
            }

            // set selected item
            ClimateModel = null;

            // set a default value of an input climate model
            InputClimateModel = new ClimateModel()
            {
                ModelData =
                    new ParticleTrack(_vectorField, 250, 4, 40, _lonPoints, _latPoints, _lonMax - _lonMin,
                        _latMax - _latMin),
                ModelDescription = "new model",
                ModelDate = new DateTime(1847, 1, 30)
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        private ClimateModel _inputClimateModel;
        public ClimateModel InputClimateModel
        {
            get { return _inputClimateModel; }
            set
            {
                _inputClimateModel = value;
                RaisePropertyChanged("InputClimateModel");
                RaisePropertyChanged("SetM");
                RaisePropertyChanged("SetT");
                RaisePropertyChanged("SetK");
            }
        }

        private ClimateModel _climateModel;
        public ClimateModel ClimateModel
        {
            get { return _climateModel; }
            set
            {
                _climateModel = value;

                if (value != null) CreateDrawingCollection(_climateModel.ModelData.ParallelTracks);
                else DrawingCollection = null;
                RaisePropertyChanged("ClimateModel");       
            }
        }

        private ObservableCollection<ClimateModel> _filterClimateModelCollection;
        private ObservableCollection<ClimateModel> _climateModelCollection;
        public ObservableCollection<ClimateModel> ClimateModelCollection
        {
            get
            {
                if (_search == string.Empty)
                    return _climateModelCollection;

                _filterClimateModelCollection = new ObservableCollection<ClimateModel>();
                var query = from dbmodel in _context.DataBaseClimateModels
                    where dbmodel.Description.Contains(_search) ||
                          (dbmodel.Date.Day + "." + dbmodel.Date.Month + "." + dbmodel.Date.Year).Contains(_search)
                    select dbmodel;

                foreach (var dbmodel in query)
                {
                    ClimateModel model = new ClimateModel
                    {
                        ModelData = new ParticleTrack(_vectorField, dbmodel.M, dbmodel.T, dbmodel.K, _lonPoints,
                            _latPoints, _lonMax - _lonMin,
                            _latMax - _latMin)
                        { ParallelTracks = ReadBlob(dbmodel.Blob) },
                        Id = dbmodel.Id,
                        ModelDate = dbmodel.Date,
                        ModelDescription = dbmodel.Description
                    };
                        
                    _filterClimateModelCollection.Add(model);
                }
                return _filterClimateModelCollection;
            }
            set
            {
                _climateModelCollection = value;
                RaisePropertyChanged("ClimateModelCollection");
            }
        }

        private DrawingCollection _drawingCollection;
        public DrawingCollection DrawingCollection
        {
            get { return _drawingCollection; }
            set
            {
                _drawingCollection = value;
                RaisePropertyChanged("DrawingCollection");
            }
        }

        private string _mistake;
        public string Mistake
        {
            get { return _mistake; }
            set
            {
                _mistake = value; 
                RaisePropertyChanged("Mistake");
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                _isEnabled = value; 
                RaisePropertyChanged("IsEnabled");
            }
        }


        private int _imageWidth;
        public int ImageWidth
        {
            get { return _imageWidth; }
            set
            {
                _imageWidth = value; 
                RaisePropertyChanged("ImageWidth");
            }
        }

        private int _imageHeight;
        public int ImageHeight
        {
            get { return _imageHeight; }
            set
            {
                _imageHeight = value; 
                RaisePropertyChanged("ImageHeight");
            }
        }

        private string _imagePath;
        public string ImagePath
        {
            get { return _imagePath; }
            set
            {
                _imagePath = value;
                RaisePropertyChanged("ImagePath");
            }
        }

        public string SetM
        {
            get
            {
                return _inputClimateModel.ModelData.M.ToString();
            }
            set
            {
                int m;
                if (int.TryParse(value, out m) && m > 10 && m < 1000)
                {
                    _inputClimateModel.ModelData.M = m;
                    RaisePropertyChanged("SetM");
                }
                
            }
        }

        public string SetT
        {
            get
            {
                return _inputClimateModel.ModelData.T.ToString();
            }
            set
            {
                int t;
                if (int.TryParse(value, out t) && t > 1 && t < 100)
                {
                    _inputClimateModel.ModelData.T = t;
                    RaisePropertyChanged("SetT");
                }
            }
        }

        public string SetK
        {
            get
            {
                return _inputClimateModel.ModelData.K.ToString();
            }
            set
            {
                int k;
                if (int.TryParse(value, out k) && k > 1 && k < 300)
                {
                    _inputClimateModel.ModelData.K = k;
                    RaisePropertyChanged("SetK");
                } 
            }
        }

        private string _search;
        public string Search
        {
            get { return _search; }
            set
            {
                _search = value; 
                ClimateModel = null;
                RaisePropertyChanged("Search");
                RaisePropertyChanged("ClimateModelCollection");
            }
        }

        private DelegateCommand _computeCollection;
        private async void computeCollection(object obj)
        {
            ClimateModel = null;
            // refresh image
            DrawingCollection = new DrawingCollection();
            IsEnabled = false;
            Mistake = "Формируется коллекция";

            var result = await Task<List<Vector>[]>.Factory.StartNew(() =>
            {
                _inputClimateModel.ModelData.ParallelTracksComputation();
                return _inputClimateModel.ModelData.ParallelTracks;
            });

            // M can be changed after computations
            SetM = _inputClimateModel.ModelData.M.ToString();

            CreateDrawingCollection(result);

            IsEnabled = true;
            Mistake = "";
        }
        public ICommand ComputeCollection
        {
            get
            {
                return _computeCollection ?? (_computeCollection = new DelegateCommand(computeCollection, o => true));
            }
        }

        public void CreateDrawingCollection(List<Vector>[] parallelTracks)
        {
            // form _drawingCollection
            _drawingCollection = new DrawingCollection();
            EllipseGeometry circle = new EllipseGeometry();

            // add point of origin
            Color c = Color.FromArgb(0, 0, 255, 0);
            circle.Center = new Point(0, 0);
            circle.RadiusX = circle.RadiusY = 0.001;
            _drawingCollection.Add(new GeometryDrawing(new SolidColorBrush(c), new Pen(), circle));

            foreach (List<Vector> track in parallelTracks)
            {
                int deltaBrightness = 255 / track.Count;
                for (int j = 0; j < track.Count; j++)
                {
                    c = Color.FromArgb(255, (byte)(255 - deltaBrightness * j),
                        (byte)(255 - deltaBrightness * j), (byte)(255 - deltaBrightness * j));
                    circle = new EllipseGeometry();
                    circle.RadiusX = circle.RadiusY = 1.5;
                    circle.Center = new Point(track[j].X * _imageWidth / (_lonMax - _lonMin),
                        (5.0 + track[j].Y) * _imageHeight / 180);
                    _drawingCollection.Add(new GeometryDrawing(new SolidColorBrush(c), new Pen(), circle));
                }
            }

            RaisePropertyChanged("DrawingCollection");
        }

        private DelegateCommand _addClimateModel;
        private void addClimateModel(object obj)
        {
            // only nonvoid parallelTracks
            if (_inputClimateModel.ModelData.ParallelTracks != null)
            {
                DataBaseClimateModel dbmodel = new DataBaseClimateModel
                {
                    Description = _inputClimateModel.ModelDescription,
                    Date = _inputClimateModel.ModelDate,
                    M = _inputClimateModel.ModelData.M,
                    T = _inputClimateModel.ModelData.T,
                    K = _inputClimateModel.ModelData.K,
                    Blob = CreateBlob(_inputClimateModel)
                };
                _context.DataBaseClimateModels.Add(dbmodel);
                _context.SaveChanges();

                _inputClimateModel.Id = dbmodel.Id; 
                _climateModelCollection.Add(_inputClimateModel);
                RaisePropertyChanged("ClimateModelCollection");

                // set a default value of an input climate model
                InputClimateModel = new ClimateModel()
                {
                    ModelData = new ParticleTrack(_vectorField, 250, 4, 50, _lonPoints, _latPoints, _lonMax - _lonMin, _latMax - _latMin),
                    ModelDescription = "new model",
                    ModelDate = new DateTime(1889, 1, 30)
                };
                Mistake = "";
            }
            else
            {
                Mistake = "Коллекция не сформирована";
            }
        }
        public ICommand AddClimateModel
        {
            get { return _addClimateModel ?? (_addClimateModel = new DelegateCommand(addClimateModel, o => true)); }
        }

        private DelegateCommand _removeClimateModel;
        private void removeClimateModel(object obj)
        {
            var query = from dbmodel in _context.DataBaseClimateModels
                where dbmodel.Id == _climateModel.Id
                select dbmodel;

            if (query.ToList().Count != 1) return;
            DataBaseClimateModel removeDbModel = _context.DataBaseClimateModels.First(dbmodel => dbmodel.Id == _climateModel.Id);
            int modelIndex = (from dbmodel in _context.DataBaseClimateModels select dbmodel.Id).ToList().IndexOf(_climateModel.Id);

            _context.DataBaseClimateModels.Remove(removeDbModel);
            _context.SaveChanges();

            ClimateModel = null;
            _climateModelCollection.RemoveAt(modelIndex);
            RaisePropertyChanged("ClimateModelCollection");
        }
        public ICommand RemoveClimateModel
        {
            get { return _removeClimateModel ?? (_removeClimateModel = new DelegateCommand(removeClimateModel, o => true)); }
        }

        private void ReadFile(string path)
        {
            FileStream fin;
            try
            {
                fin = new FileStream(path, FileMode.Open);
            }
            catch (Exception)
            {
                Mistake = "Ошибка открытия файла";
                Trace.WriteLine("Ошибка открытия файла");
                return;
            }
            StreamReader fstrIn = new StreamReader(fin);

            try
            {
                string str = fstrIn.ReadLine();
                string[] strArr = str.Split(',');
                _latMin = int.Parse(strArr[0]);
                _latMax = int.Parse(strArr[1]);
                _latPoints = int.Parse(strArr[2]);

                str = fstrIn.ReadLine();
                strArr = str.Split(',');
                _lonMin = int.Parse(strArr[0]);
                _lonMax = int.Parse(strArr[1]);
                _lonPoints = int.Parse(strArr[2]);

                _vectorField = new Vector[_lonPoints, _latPoints];
                for (int i = 0; i < _lonPoints; i++)
                {
                    str = fstrIn.ReadLine();
                    strArr = str.Split(',');
                    for (int j = 0; j < _latPoints; j++)
                    {
                        _vectorField[i, j].X = double.Parse(strArr[j].Replace('.', ','));
                    }
                }

                for (int i = 0; i < _lonPoints; i++)
                {
                    str = fstrIn.ReadLine();
                    strArr = str.Split(',');
                    for (int j = 0; j < _latPoints; j++)
                    {
                        _vectorField[i, j].Y = double.Parse(strArr[j].Replace('.', ','));
                    }
                }

            }
            catch (Exception ex)
            {
                Mistake = "Ошибка ввода-вывода";
                Trace.WriteLine("Ошибка ввода-вывода");
                Trace.WriteLine(ex.Message);
                return;
            }
            finally
            {
                fstrIn.Close();
                fin.Close();
            }
        }

        private byte[] CreateBlob(ClimateModel model)
        {
            // add parallel tracks in a blob
            byte[] blob = new byte[0];
            
            // add an amount of tracks
            byte[] tracksAmountArray = BitConverter.GetBytes(model.ModelData.ParallelTracks.Length); 
            blob = CombineByteArrays(blob.Length + tracksAmountArray.Length, blob, tracksAmountArray);

            // add all tracks
            foreach (List<Vector> track in model.ModelData.ParallelTracks)
            {
                byte[] particleTrackArray = new byte[sizeof(int) + 2 * sizeof(double) * track.Count];
                // add a count
                BitConverter.GetBytes(track.Count).CopyTo(particleTrackArray, 0); 
                
                for (int i = 0; i < track.Count; i++)
                {
                    BitConverter.GetBytes(track[i].X).CopyTo(particleTrackArray, sizeof(int) + i * 2 * sizeof(double));
                    BitConverter.GetBytes(track[i].Y).CopyTo(particleTrackArray, sizeof(int) + i * 2 * sizeof(double) + sizeof(double));
                }

                blob = CombineByteArrays(blob.Length + particleTrackArray.Length, blob, particleTrackArray);
            }

            return blob;
        }

        private List<Vector>[] ReadBlob(byte[] blob)
        {
            int index = 0;

            // read an amount of tracks
            List<Vector>[] parallelTracks = new List<Vector>[BitConverter.ToInt32(blob, index)];
            index += sizeof(int);

            // read all tracks
            for (int i = 0; i < parallelTracks.Length; i++)
            {
                parallelTracks[i] = new List<Vector>();
                // read a count
                int count = BitConverter.ToInt32(blob, index);
                index += sizeof(int);
                for (int j = 0; j < count; j++)
                {
                    parallelTracks[i].Add(new Vector(BitConverter.ToDouble(blob, index), BitConverter.ToDouble(blob, index + sizeof(double))));
                    index += 2 * sizeof(double);
                }
            }

            return parallelTracks;
        }

        private byte[] CombineByteArrays(int length, byte[] byteArray1, byte[] byteArray2)
        {
            byte[] result = new byte[length];
            byteArray1.CopyTo(result, 0);
            byteArray2.CopyTo(result, byteArray1.Length);

            return result;
        }
    }
}
