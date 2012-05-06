using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearAlgebra.Generic;

namespace Uncas.Sandbox.Fraud
{
    public class LogisticRegression
    {
        public static readonly bool UseSecondOrder = true;

        public Vector<double> Iterate<T>(
            IList<Sample<T>> samples,
            IList<Feature<T>> features,
            double stepSize,
            int iterations)
        {
            IList<Dimension<T>> dimensions = GetDimensions(features);
            Vector<double> thetas = GetInitialGuessAtTheta(dimensions);
            Console.WriteLine("Iterations:");
            for (int iteration = 0; iteration < iterations; iteration++)
                thetas = GradientDescent(samples, thetas, stepSize, iteration);
            OutputBestFit(dimensions, thetas);
            OutputDeviations(samples);
            return thetas;
        }

        private static Vector<double> GradientDescent<T>(
            IList<Sample<T>> samples,
            Vector<double> thetas,
            double stepSize,
            int iteration)
        {
            Vector<double> sums = new DenseVector(thetas.Count, 0d);
            foreach (var sample in samples)
            {
                sample.Probability = GetProbability(sample, thetas);
                double deviation = sample.Probability - (sample.Match ? 1d : 0d);
                sums += deviation*sample.Dimensions;
            }

            thetas += -stepSize*sums;
            if (iteration == 0 || (iteration + 1)%10 == 0)
                OutputTotalDeviationAtIteration(samples, iteration);
            return thetas;
        }

        private static void OutputTotalDeviationAtIteration<T>(IList<Sample<T>> samples, int iteration)
        {
            double deviationSquared = samples.Select(s => Math.Pow(s.Deviation, 2d)).Sum();
            double deviation = Math.Sqrt(deviationSquared/samples.Count);
            Console.WriteLine("  {0}: standard deviation={1:P3}", iteration + 1, deviation);
        }

        private static Vector<double> GetInitialGuessAtTheta<T>(IEnumerable<Dimension<T>> dimensions)
        {
            return new DenseVector(dimensions.Select(d => d.GetInitialGuess()).ToArray());
        }

        private static void OutputDeviations<T>(IEnumerable<Sample<T>> samples)
        {
            const double deviationThreshold = 0.001d;
            IEnumerable<Sample<T>> deviatingSamples =
                samples.Where(x => Math.Abs(x.Deviation) > deviationThreshold).ToList();
            if (!deviatingSamples.Any())
                return;

            Console.WriteLine("Deviations above {0:P1}:", deviationThreshold);
            foreach (var sample in deviatingSamples.OrderByDescending(x => Math.Abs(x.Deviation)))
                Console.WriteLine(
                    "  {0}, {1:P2}, {2:P2}, {3}",
                    sample.Match,
                    sample.Probability,
                    sample.Deviation,
                    sample.Identifier);
        }

        private static void OutputBestFit<T>(
            IList<Dimension<T>> dimensions,
            Vector<double> thetas)
        {
            Console.WriteLine("Dimensions and best fit:");
            for (int dimensionIndex = 0; dimensionIndex < dimensions.Count; dimensionIndex++)
            {
                double theta = thetas[dimensionIndex];
                Dimension<T> dimension = dimensions[dimensionIndex];
                Console.WriteLine("  Theta={1:N3}, Feature: {0}", dimension.Description, theta);
            }
        }

        private static IList<Dimension<T>> GetDimensions<T>(IList<Feature<T>> features)
        {
            var dimensions = new List<Dimension<T>>();

            // Zeroth order:
            dimensions.Add(new Dimension<T>());

            // First order:
            dimensions.AddRange(features.Select(feature => new Dimension<T>(feature)));

            if (!UseSecondOrder)
                return dimensions;

            // Second order:
            int numberOfFeatures = features.Count;
            for (int featureIndex1 = 0; featureIndex1 < numberOfFeatures; featureIndex1++)
            {
                for (int featureIndex2 = featureIndex1; featureIndex2 < numberOfFeatures; featureIndex2++)
                    dimensions.Add(new Dimension<T>(features[featureIndex1], features[featureIndex2]));
            }

            return dimensions;
        }

        private static double GetProbability<T>(
            Sample<T> sample,
            Vector<double> theta)
        {
            double thetaX =
                sample.Dimensions.Select((dimension, dimensionIndex) => theta.ElementAt(dimensionIndex)*dimension).Sum();
            return SpecialFunctions.Logistic(thetaX);
        }
    }
}