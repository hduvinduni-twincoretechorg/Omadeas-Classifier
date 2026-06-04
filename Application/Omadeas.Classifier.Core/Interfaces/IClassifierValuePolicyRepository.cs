using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierValuePolicyRepository
{
    /// <summary>Returns the policy for a classifier, or null if none exists.</summary>
    Task<ClassifierValuePolicyDto?> GetClassifierValuePolicyByClassifierAsync(Guid classifierId);

    /// <summary>Inserts a policy and returns the created row.</summary>
    Task<ClassifierValuePolicyDto> AddClassifierValuePolicyAsync(ClassifierValuePolicyDto policy);

    /// <summary>Updates the policy for a classifier (1:1).</summary>
    Task UpdateClassifierValuePolicyAsync(ClassifierValuePolicyDto policy);
}
