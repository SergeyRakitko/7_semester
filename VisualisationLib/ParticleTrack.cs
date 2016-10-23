using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VisualisationLib
{
    public class ParticleTrack
    {
        private readonly int _n1, _n2;
        private readonly double _l1, _l2, _h1, _h2;
        public int M { get; set; }
        public int T { get; set; }
        public int K { get; set; }
        private double _alpha, _beta;
        private readonly Vector[,] _vectorField;
        private double _deltaT;
        public List<Vector>[] Tracks;
        public List<Vector>[] ParallelTracks;

        public ParticleTrack(Vector[,] vectorFieldP, int m, int t, int k, int n1, int n2, double l1, double l2)
        {
            try
            {
                _n1 = n1 - 1;
                _n2 = n2 - 1;
                _l1 = l1;
                _l2 = l2;
                _h1 = _l1 / _n1;
                _h2 = _l2 / _n2;
                M = m;
                T = t;
                K = k;

                _vectorField = new Vector[_n1 + 1, _n2 + 1];
                // инициализация векторного поля
                for (int i = 0; i < _n1 + 1; i++)
                    for (int j = 0; j < _n2 + 1; j++)
                        _vectorField[i, j] = vectorFieldP[i, j];              
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\nConstructor");
            }
        }

        public void PreparatoryInitialization(string method)
        {
            _deltaT = (double)T / K;

            // получение оптимального значения М для данных размеров сетки
            int mX = (int)Math.Round(Math.Sqrt((double)M * _n1 / _n2));
            int mY = (int)Math.Round((double)mX * _n2 / _n1);
            double hX = _n1 / (mX + 1.0);
            double hY = _n2 / (mY + 1.0);

            M = mX * mY;

            // распределение точек по сетке
            if (method == "TracksComputation")
            {
                Tracks = new List<Vector>[M];
                for (int i = 0; i < M; i++)
                {
                    Tracks[i] = new List<Vector>();
                }

                for (int i = 1; i < mX + 1; i++)
                {
                    for (int j = 1; j < mY + 1; j++)
                    {
                        Tracks[(i - 1) * mY + j - 1].Add(new Vector(i * hX * _h1, j * hY * _h2));
                    }
                }
            }
            if (method == "ParallelTracksComputation")
            {
                ParallelTracks = new List<Vector>[M];
                for (int i = 0; i < M; i++)
                {
                    ParallelTracks[i] = new List<Vector>();
                }

                for (int i = 1; i < mX + 1; i++)
                {
                    for (int j = 1; j < mY + 1; j++)
                    {
                        ParallelTracks[(i - 1) * mY + j - 1].Add(new Vector(i * hX * _h1, j * hY * _h2));
                    }
                }
            }
            
        }

        public void TracksComputation()
        {
            try
            {
                PreparatoryInitialization("TracksComputation");

                Vector k1, k2, k3, k4;
                for (int i = 0; i < M; i++)
                {
                    for (int j = 1; j < K + 1; j++)
                    {
                        double previousParticleX = Tracks[i][j - 1].X, previousParticleY = Tracks[i][j - 1].Y;

                        if (previousParticleX < 0 || previousParticleX >= _l1
                        || previousParticleY < 0 || previousParticleY >= _l2) break;
                        k1 = _deltaT * GetVectorField(Tracks[i][j - 1]);

                        if ((previousParticleX + k1.X / 2) < 0 || (previousParticleX + k1.X / 2) >= _l1
                            || (previousParticleY + k1.Y / 2) < 0 || (previousParticleY + k1.Y / 2) >= _l2) break;
                        k2 = _deltaT * GetVectorField(Tracks[i][j - 1] + k1 / 2);

                        if ((previousParticleX + k2.X / 2) < 0 || (previousParticleX + k2.X / 2) >= _l1
                            || (previousParticleY + k2.Y / 2) < 0 || (previousParticleY + k2.Y / 2) >= _l2) break;
                        k3 = _deltaT * GetVectorField(Tracks[i][j - 1] + k2 / 2);

                        if ((previousParticleX + k3.X) < 0 || (previousParticleX + k3.X) >= _l1
                            || (previousParticleY + k3.Y) < 0 || (previousParticleY + k3.Y) >= _l2) break;
                        k4 = _deltaT * GetVectorField(Tracks[i][j - 1] + k3);

                        Tracks[i].Add(Tracks[i][j - 1] + (k1 + 2 * k2 + 2 * k3 + k4) / 6);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\nTracksComputation");
            }

        }

        public void ParallelTracksComputation()
        {
            try
            {
                PreparatoryInitialization("ParallelTracksComputation");

                Parallel.For(0, M, i =>
                {
                    Vector k1, k2, k3, k4;
                    for (int j = 1; j < K + 1; j++)
                    {
                        double previousParticleX = ParallelTracks[i][j - 1].X, previousParticleY = ParallelTracks[i][j - 1].Y;

                        if (previousParticleX < 0 || previousParticleX >= _l1
                        || previousParticleY < 0 || previousParticleY >= _l2) break;
                        k1 = _deltaT * GetVectorField(ParallelTracks[i][j - 1]);

                        if ((previousParticleX + k1.X / 2) < 0 || (previousParticleX + k1.X / 2) >= _l1
                            || (previousParticleY + k1.Y / 2) < 0 || (previousParticleY + k1.Y / 2) >= _l2) break;
                        k2 = _deltaT * GetVectorField(ParallelTracks[i][j - 1] + k1 / 2);

                        if ((previousParticleX + k2.X / 2) < 0 || (previousParticleX + k2.X / 2) >= _l1
                            || (previousParticleY + k2.Y / 2) < 0 || (previousParticleY + k2.Y / 2) >= _l2) break;
                        k3 = _deltaT * GetVectorField(ParallelTracks[i][j - 1] + k2 / 2);

                        if ((previousParticleX + k3.X) < 0 || (previousParticleX + k3.X) >= _l1
                            || (previousParticleY + k3.Y) < 0 || (previousParticleY + k3.Y) >= _l2) break;
                        k4 = _deltaT * GetVectorField(ParallelTracks[i][j - 1] + k3);

                        ParallelTracks[i].Add(ParallelTracks[i][j - 1] + (k1 + 2 * k2 + 2 * k3 + k4) / 6);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message + "\nParallelTracksComputation");
            }
        }

        private Vector GetVectorField(Vector vec)
        {
            int i = Convert.ToInt32(Math.Floor(vec.X / _h1));
            int j = Convert.ToInt32(Math.Floor(vec.Y / _h2));
            _alpha = (vec.X - i * _h1) / _h1;
            _beta = (vec.Y - j * _h2) / _h2;

            return new Vector((1 - _alpha) * (1 - _beta) * _vectorField[i, j].X + (1 - _alpha) * _beta * _vectorField[i, j + 1].X +
                _alpha * (1 - _beta) * _vectorField[i + 1, j].X + _alpha * _beta * _vectorField[i + 1, j + 1].X,
                (1 - _alpha) * (1 - _beta) * _vectorField[i, j].Y + (1 - _alpha) * _beta * _vectorField[i, j + 1].Y +
                _alpha * (1 - _beta) * _vectorField[i + 1, j].Y + _alpha * _beta * _vectorField[i + 1, j + 1].Y);
        }
    }
}
