# PhotoLoja dos rapazes

Editor derivado do PhotoCraft, com marca, instalação e perfil próprios.
Esta edição parte da base PhotoCraft v0.23.1 e inclui os ajustes locais da 0.23.3.
Os créditos e avisos de licença do projeto original acompanham o código e o instalador.

## Integrar atualizações do PhotoCraft

A branch `photoloja` mantém as mudanças locais no histórico. O remoto `upstream`
aponta para `https://github.com/storytold/photocraft.git`.

Busque as atualizações, crie uma branch de integração e escolha uma tag ou commit.

```powershell
git fetch --unshallow upstream
git fetch upstream --tags
git switch -c integrar-photocraft
git merge <tag-ou-commit>
```

Use `--unshallow` apenas se `git rev-parse --is-shallow-repository` retornar
`true`. Para uma correção isolada, use `git cherry-pick <commit>` na branch
de integração. Se ambos os projetos alterarem o mesmo trecho, resolva o
conflito preservando os comportamentos desejados e teste antes de integrar
a branch ao PhotoLoja. A instalação do PhotoLoja não consulta as releases
do PhotoCraft.

A correção do Windows Ink inclui o patch em `packaging/photoloja/winit-windows-ink.patch`.
Aplique-o ao checkout do winit fixado pelo Cargo se a dependência ainda não
contiver a correção. Revise o patch ao atualizar essa dependência.
