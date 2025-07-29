using System;
using System.Collections.Generic;

namespace NostrSure.Domain.Entities;

public sealed record NostrEvent(
    string Id,
    Pubkey Pubkey,
    DateTimeOffset CreatedAt,
    int Kind,
    IReadOnlyList<IReadOnlyList<string>> Tags,
    string Content,
    string Sig
);