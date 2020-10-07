﻿using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// Generates samples by choosing one option from a list of choices
    /// </summary>
    /// <typeparam name="T">The sample type of the categorical parameter</typeparam>
    [Serializable]
    public abstract class CategoricalParameter<T> : CategoricalParameterBase
    {
        [SerializeField] internal bool uniform = true;
        [SerializeReference] ISampler m_Sampler = new UniformSampler(0f, 1f);

        [SerializeField] List<T> m_Categories = new List<T>();
        float[] m_NormalizedProbabilities;

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        internal override IEnumerable<ISampler> samplers
        {
            get { yield return m_Sampler; }
        }

        /// <summary>
        /// The sample type generated by this parameter
        /// </summary>
        public sealed override Type sampleType => typeof(T);

        /// <summary>
        /// Returns the category stored at the specified index
        /// </summary>
        /// <param name="index">The index of the category to lookup</param>
        /// <returns>The category stored at the specified index</returns>
        public T GetCategory(int index) => m_Categories[index];

        /// <summary>
        /// Returns the probability value stored at the specified index
        /// </summary>
        /// <param name="index">The index of the probability value to lookup</param>
        /// <returns>The probability value stored at the specified index</returns>
        public float GetProbability(int index) => probabilities[index];

        /// <summary>
        /// Updates this parameter's list of categorical options
        /// </summary>
        /// <param name="categoricalOptions">The categorical options to configure</param>
        public void SetOptions(IEnumerable<T> categoricalOptions)
        {
            m_Categories.Clear();
            probabilities.Clear();
            foreach (var category in categoricalOptions)
                AddOption(category, 1f);
            NormalizeProbabilities();
        }

        /// <summary>
        /// Updates this parameter's list of categorical options
        /// </summary>
        /// <param name="categoricalOptions">The categorical options to configure</param>
        public void SetOptions(IEnumerable<(T, float)> categoricalOptions)
        {
            m_Categories.Clear();
            probabilities.Clear();
            foreach (var (category, probability) in categoricalOptions)
                AddOption(category, probability);
            NormalizeProbabilities();
        }

        void AddOption(T option, float probability)
        {
            m_Categories.Add(option);
            probabilities.Add(probability);
        }

        /// <summary>
        /// Returns a list of the potential categories this parameter can generate
        /// </summary>
        public IReadOnlyList<(T, float)> categories
        {
            get
            {
                var catOptions = new List<(T, float)>(m_Categories.Count);
                for (var i = 0; i < catOptions.Count; i++)
                    catOptions.Add((m_Categories[i], probabilities[i]));
                return catOptions;
            }
        }

        /// <summary>
        /// Validates the categorical probabilities assigned to this parameter
        /// </summary>
        /// <exception cref="ParameterValidationException"></exception>
        public override void Validate()
        {
            base.Validate();
            if (!uniform)
            {
                if (probabilities.Count != m_Categories.Count)
                    throw new ParameterValidationException("Number of options must be equal to the number of probabilities");
                NormalizeProbabilities();
            }
        }

        void NormalizeProbabilities()
        {
            var totalProbability = 0f;
            for (var i = 0; i < probabilities.Count; i++)
            {
                var probability = probabilities[i];
                if (probability < 0f)
                    throw new ParameterValidationException($"Found negative probability at index {i}");
                totalProbability += probability;
            }

            if (totalProbability <= 0f)
                throw new ParameterValidationException("Total probability must be greater than 0");

            var sum = 0f;
            m_NormalizedProbabilities = new float[probabilities.Count];
            for (var i = 0; i < probabilities.Count; i++)
            {
                sum += probabilities[i] / totalProbability;
                m_NormalizedProbabilities[i] = sum;
            }
        }

        int BinarySearch(float key) {
            var minNum = 0;
            var maxNum = m_NormalizedProbabilities.Length - 1;

            while (minNum <= maxNum) {
                var mid = (minNum + maxNum) / 2;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (key == m_NormalizedProbabilities[mid]) {
                    return ++mid;
                }
                if (key < m_NormalizedProbabilities[mid]) {
                    maxNum = mid - 1;
                }
                else {
                    minNum = mid + 1;
                }
            }
            return minNum;
        }

        /// <summary>
        /// Generates a sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public T Sample()
        {
            var randomValue = m_Sampler.Sample();
            return uniform
                ? m_Categories[(int)(randomValue * m_Categories.Count)]
                : m_Categories[BinarySearch(randomValue)];
        }

        /// <summary>
        /// Generates a generic sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override object GenericSample()
        {
            return Sample();
        }
    }
}
