using System;

namespace ProjectChimera.Systems.Tutorial
{
    /// <summary>
    /// Interface for Tutorial system management.
    /// </summary>
    public interface ITutorialManager
    {
        #region Properties

        bool IsTutorialActive { get; }
        bool IsTutorialCompleted { get; }
        int CurrentStepIndex { get; }
        TutorialProgress Progress { get; }

        #endregion

        #region Events

        event Action OnTutorialStarted;
        event Action OnTutorialCompleted;
        event Action<int> OnStepStarted;
        event Action<int> OnStepCompleted;
        event Action OnTutorialSkipped;

        #endregion

        #region Tutorial Control

        /// <summary>
        /// Starts the tutorial from beginning.
        /// </summary>
        void StartTutorial();

        /// <summary>
        /// Skips the tutorial entirely.
        /// </summary>
        void SkipTutorial();

        /// <summary>
        /// Advances to next tutorial step.
        /// </summary>
        void NextStep();

        /// <summary>
        /// Goes back to previous tutorial step.
        /// </summary>
        void PreviousStep();

        /// <summary>
        /// Completes current step and advances.
        /// </summary>
        void CompleteCurrentStep();

        #endregion

        #region Step Management

        /// <summary>
        /// Gets current tutorial step.
        /// </summary>
        TutorialStep GetCurrentStep();

        /// <summary>
        /// Checks if specific step is completed.
        /// </summary>
        bool IsStepCompleted(int stepIndex);

        #endregion
    }
}
