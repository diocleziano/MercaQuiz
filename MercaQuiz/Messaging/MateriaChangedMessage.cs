using CommunityToolkit.Mvvm.Messaging.Messages;
using MercaQuiz.MVVM.Models; // o MercaQuiz.MVVM.Models

namespace MercaQuiz.Messaging;


public sealed class MateriaChangedMessage : ValueChangedMessage<Materia>
{
    public MateriaChangedMessage(Materia value) : base(value) { }
}
