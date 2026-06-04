using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierProfileHistoryRepository
{
    /// <summary>Appends a profile definition history event and returns the created row.</summary>
    Task<ClassifierProfileHistoryDto> AddClassifierProfileHistoryAsync(ClassifierProfileHistoryDto history);

    /// <summary>Returns the history for a profile, most recent first.</summary>
    Task<IEnumerable<ClassifierProfileHistoryDto>> GetClassifierProfileHistoryAsync(Guid profileId);
}
