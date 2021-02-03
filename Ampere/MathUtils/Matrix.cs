using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ampere.Base;
using Ampere.Base.Attributes;

namespace Ampere.MathUtils
{
    /// <summary>
    /// A class representing a mathematical Matrix. Creates a rectangular
    /// array of rows and columns with numbers as elements. The Matrix
    /// class includes mathematical matrix operations to manipulate it.
    /// </summary>
    [Beta]
    public class Matrix : IMatrixer<double>, IIndexableDouble<int, double>
    {
        private const double Tolerance = 0.000000000001;

        /// <inheritdoc cref="IMatrixer{T}"/>
        public double[,] Values { get; }

        /// <inheritdoc cref="IMatrixer{T}"/>
        public int Rows { get; }

        /// <inheritdoc cref="IMatrixer{T}"/>
        public int Cols { get; }

        /// <summary>
        /// Creates an instance of a Matrix given rows and columns.
        /// </summary>
        /// <param name="rows">The number of rows in this Matrix</param>
        /// <param name="cols">The number of columns in this Matrix</param>
        public Matrix(int rows, int cols)
        {
            Values = new double[rows, cols];
            Rows = rows;
            Cols = cols;
        }

        /// <summary>
        /// Creates an instance of a Matrix given a 2D array. 
        /// </summary>
        /// <param name="matrix">A 2D array of doubles</param>
        public Matrix(double[,] matrix)
        {
            matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
            Values = matrix;
            Rows = matrix.GetLength(0);
            Cols = matrix.GetLength(1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public double this[int row, int col]
        {
            get => Values[row, col];
  
            set => Values[row, col] = value;
        }

        /// <inheritdoc cref="IMatrixer{T}"/>
        public Matrix Transpose()
        {
            var cp = new Matrix(Cols, Rows);

            for (var r = 0; r < Rows; r++)
            {
                for (var c = 0; c < Cols; c++)
                {
                    cp[c, r] = Values[r, c];
                }
            }
            return cp;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool EqualDimension(Matrix one, Matrix two)
        {
            one = one ?? throw new ArgumentNullException(nameof(one));
            two = two ?? throw new ArgumentNullException(nameof(two));
            return one.Rows == two.Rows && one.Cols == two.Cols;
        }

        private protected static Matrix DoTwoMatrixScalar(Matrix one, Matrix two, Func<double, double, double> action)
        {
            var n = new Matrix(one.Rows, two.Cols);

            if (!EqualDimension(one, two))
            {
                throw new MatrixPropertyException("Both matrices must be of equal dimensions");
            }

            for (var r = 0; r < one.Rows; r++)
            {
                for (var c = 0; c < two.Cols; c++)
                {
                    n[r, c] = action(one[r, c], two[r, c]);
                }
            }
            return n;
        }

        private protected static Matrix DoScalar(Matrix m, double? sc, Func<double, double?, double> action)
        {
            for (var r = 0; r < m.Rows; r++)
            {
                for (var c = 0; c < m.Cols; c++)
                {
                    m[r, c] = action(m[r, c], sc);
                }
            }
            return m;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix operator +(Matrix m, double scalar) => DoScalar(m, scalar, (val, sc) => val + (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix operator +(double scalar, Matrix m) => DoScalar(m, scalar, (val, sc) => val + (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix operator -(Matrix m, double scalar) => DoScalar(m, scalar, (val, sc) => val - (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix operator -(double scalar, Matrix m) => DoScalar(m, scalar, (val, sc) => val - (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix m, double scalar) => DoScalar(m, scalar, (val, sc) => val * (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix operator *(double scalar, Matrix m) => DoScalar(m, scalar, (val, sc) => val * (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix operator /(Matrix m, double scalar) => DoScalar(m, scalar, (val, sc) => val / (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix operator /(double scalar, Matrix m) => DoScalar(m, scalar, (sc, val) => sc / (double)val);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix operator %(Matrix m, double scalar) => DoScalar(m, scalar, (val, sc) => val % (double)sc);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scalar"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix operator %(double scalar, Matrix m) => DoScalar(m, scalar, (sc, val) => sc % (double)val);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix operator !(Matrix m) => DoScalar(m, null, (val, sc) => val - (val * 2));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static Matrix operator +(Matrix one, Matrix two)
        {
            one = one ?? throw new ArgumentNullException(nameof(one));
            two = two ?? throw new ArgumentNullException(nameof(two));

            return DoTwoMatrixScalar(one, two, (ac, ac2) => ac + ac2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static Matrix operator -(Matrix one, Matrix two)
        {
            one = one ?? throw new ArgumentNullException(nameof(one));
            two = two ?? throw new ArgumentNullException(nameof(two));

            return DoTwoMatrixScalar(one, two, (ac, ac2) => ac - ac2);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix one, Matrix two)
        {
            one = one ?? throw new ArgumentNullException(nameof(one));
            two = two ?? throw new ArgumentNullException(nameof(two));

            var cp = new Matrix(one.Rows, two.Cols);

            if (one.Cols != two.Rows)
            {
                throw new MatrixPropertyException($"Found: Matrix one columns: {one.Cols}, Matrix Two rows: {one.Cols} "
                                                  + "but required Matrix one columns == Matrix two rows");
            }

            for (var i = 0; i < one.Rows; i++)
            {
                for (var j = 0; j < two.Cols; j++)
                {
                    for (var k = 0; k < one.Cols; k++)
                    {
                        cp[i, j] += one[i, k] * two[k, j];
                    }
                }
            }
            return cp;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator ==(Matrix one, Matrix two)
        {
            if(one is null || two is null)
            {
                return false;
            }
            if (!EqualDimension(one, two))
                return false;

            for(var i = 0; i < one.Rows; i++)
            {
                for(var j = 0; j < one.Cols; j++)
                {
                    if (Math.Abs(one[i, j] - two[i, j]) > Tolerance) 
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="one"></param>
        /// <param name="two"></param>
        /// <returns></returns>
        public static bool operator !=(Matrix one, Matrix two)
        {
            if (one is null || two is null)
            {
                return true;
            }
            if (!EqualDimension(one, two))
                return true;

            for (var i = 0; i < one.Rows; i++)
            {
                for (var j = 0; j < one.Cols; j++)
                {
                    if (Math.Abs(one[i, j] - two[i, j]) > Tolerance)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Add(Matrix left, double scalar)
           => left + scalar;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix Add(Matrix left, Matrix right)
           => left + right;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Subtract(Matrix left, double scalar)
            => left + scalar;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix Subtract(Matrix left, Matrix right)
            => left + right;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Multiply(Matrix left, double scalar)
            => left * scalar;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Matrix DotProduct(Matrix left, Matrix right)
            => left * right;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Divide(Matrix left, double scalar)
            => left / scalar;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="scalar"></param>
        /// <returns></returns>
        public static Matrix Mod(Matrix left, double scalar)
            => left % scalar;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Matrix Negate(Matrix m)
            => !m;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((Matrix) obj);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(Matrix other)
        {
            return this == other;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Values, Rows, Cols);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsRowVector() => Rows == 1;
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsColumnVector() => Cols == 1;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsSquareVector() => Rows == Cols;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<double> GetEnumerator()
        {
            for (var r = 0; r < Rows; r++)
            {
                for (var c = 0; c < Cols; c++)
                {
                    yield return Values[Rows, Cols];
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc cref="IMatrixer{T}"/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Rows; i++)
            {
                for (var j = 0; j < Cols; j++)
                {
                    sb.Append($"{Values[i, j]}\t");
                }
                sb.Append(Environment.NewLine + Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}