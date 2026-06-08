using Omadeas.Classifier.Core.DTOs;

namespace Omadeas.Classifier.Core.Interfaces;

public interface IClassifierHistoryRepository
{
    /// <summary>Appends a classifier definition history event and returns the created row.</summary>
    Task<ClassifierHistoryDto> AddClassifierHistoryAsync(ClassifierHistoryDto history);

    /// <summary>Returns the history for a classifier, most recent first.</summary>
    Task<IEnumerable<ClassifierHistoryDto>> GetClassifierHistoryAsync(Guid classifierId);
}
