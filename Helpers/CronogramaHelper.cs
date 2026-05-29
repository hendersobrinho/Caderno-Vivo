using CadernoVivo.Data;
using CadernoVivo.Models;
using Microsoft.EntityFrameworkCore;

namespace CadernoVivo.Helpers;

public static class CronogramaHelper
{
    public static async Task<int> AgendarSessoes(AppDbContext db, Projeto projeto, CronogramaProjeto cronograma)
    {
        if (cronograma.Recorrencia == null || cronograma.Recorrencia.Dias.Count == 0)
            return 0;

        var concluidas = await db.BloquesEstudo.CountAsync(b =>
            b.ProjetoId == projeto.Id &&
            (b.Status == StatusBloco.Concluido || b.Status == StatusBloco.Parcial));

        var futuros = await db.BloquesEstudo
            .Where(b => b.ProjetoId == projeto.Id &&
                        b.Status == StatusBloco.Agendado &&
                        b.Data >= DateTime.Today)
            .ToListAsync();
        db.BloquesEstudo.RemoveRange(futuros);

        var sessoesRestantes = cronograma.Sessoes.Skip(concluidas).ToList();
        if (sessoesRestantes.Count == 0) return 0;

        var datas = ProximasOcorrencias(cronograma.Recorrencia.Dias, sessoesRestantes.Count);

        for (int i = 0; i < sessoesRestantes.Count; i++)
        {
            db.BloquesEstudo.Add(new BlocoEstudo
            {
                Data = datas[i],
                HoraInicio = cronograma.Recorrencia.HoraInicio,
                HoraFim = cronograma.Recorrencia.HoraFim,
                Titulo = sessoesRestantes[i].Titulo,
                Descricao = sessoesRestantes[i].Descricao,
                Modulo = "Projeto",
                ProjetoId = projeto.Id,
                MateriaId = projeto.MateriaId,
                Status = StatusBloco.Agendado
            });
        }

        await db.SaveChangesAsync();
        return sessoesRestantes.Count;
    }

    public static List<DateTime> ProximasOcorrencias(List<int> diasSemana, int quantidade)
    {
        var resultado = new List<DateTime>();
        var data = DateTime.Today.AddDays(1);
        while (resultado.Count < quantidade)
        {
            if (diasSemana.Contains((int)data.DayOfWeek))
                resultado.Add(data);
            data = data.AddDays(1);
        }
        return resultado;
    }

    public static int NomeDiaParaInt(string nome) => nome.Trim().ToLower() switch
    {
        "domingo"   or "sunday"    => 0,
        "segunda"   or "monday"    => 1,
        "terça"     or "terca"     or "tuesday"  => 2,
        "quarta"    or "wednesday" => 3,
        "quinta"    or "thursday"  => 4,
        "sexta"     or "friday"    => 5,
        "sábado"    or "sabado"    or "saturday" => 6,
        _ => int.TryParse(nome, out var n) ? n : -1
    };
}
