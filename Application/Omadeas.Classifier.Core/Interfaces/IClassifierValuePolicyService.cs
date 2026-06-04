using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierValuePolicyService
{
    /// <summary>Returns the policy for a classifier. Throws NotFoundException if the classifier or policy is missing.</summary>
    Task<ClassifierValuePolicyDto> GetClassifierValuePolicyAsync(Guid classifierId);

    /// <summary>
    /// Creates the policy for a classifier that doesn't yet have one (validated + coherence-checked)
    /// and writes a 'policy_edit' history row. Most classifiers already have a policy auto-created
    /// at classifier-creation time.
    /// </summary>
    Task<ClassifierValuePolicyDto> CreateClassifierValuePolicyAsync(Guid classifierId, ClassifierValuePolicyDto policy);

    /// <summary>Updates the policy for a classifier and writes a 'policy_edit' history row.</summary>
    Task UpdateClassifierValuePolicyAsync(Guid classifierId, ClassifierValuePolicyDto policy);
}
