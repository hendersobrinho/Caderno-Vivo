using System.Text.Json.Serialization;

namespace CadernoVivo.Models;

public class CronogramaProjeto
{
    [JsonPropertyName("recorrencia")]
    public RecorrenciaCronograma? Recorrencia { get; set; }

    [JsonPropertyName("sessoes")]
    public List<SessaoCronograma> Sessoes { get; set; } = [];
}

public class RecorrenciaCronograma
{
    // DayOfWeek como inteiros: 0=Domingo, 1=Segunda, ..., 6=Sábado
    [JsonPropertyName("dias")]
    public List<int> Dias { get; set; } = [];

    [JsonPropertyName("hora_inicio")]
    public string HoraInicio { get; set; } = "19:00";

    [JsonPropertyName("hora_fim")]
    public string HoraFim { get; set; } = "21:00";

    public string DescricaoDias()
    {
        string[] nomes = ["Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado"];
        var labels = Dias
            .Where(d => d >= 0 && d < 7)
            .OrderBy(d => d)
            .Select(d => nomes[d]);
        return string.Join(", ", labels);
    }
}

public class SessaoCronograma
{
    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = "";

    [JsonPropertyName("descricao")]
    public string? Descricao { get; set; }
}
