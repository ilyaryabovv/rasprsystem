namespace ManagerService.Dto;

public record SortLevelDto(int Threads, List<List<int>> Runs);

public record SortedManagersDto(
    int Threads,
    double ElapsedMilliseconds,
    List<ManagerResponseDto> Items,
    List<SortLevelDto> Levels);
