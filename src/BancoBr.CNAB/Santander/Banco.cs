﻿using BancoBr.CNAB.Base;
using BancoBr.CNAB.Febraban;
using BancoBr.Common.Core;
using BancoBr.Common.Enums;
using BancoBr.Common.Instances;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoBr.CNAB.Santander
{
    public class Banco : Base.Banco
    {
        public Banco(Correntista empresa)
            : base(empresa, 33, "Banco Santander", 60) 
        {     
        }

        #region ::. Instancias .::

        internal override RegistroDetalheBase NovoSegmentoB(TipoLancamentoEnum tipoLancamento)
        {
            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                    return new SegmentoB_Transferencia(this);
                case TipoLancamentoEnum.PIXTransferencia:
                    return new SegmentoB_PIX(this);
                default:
                    throw new Exception("Tipo de lançamento não implementado");
            }
        }

        #endregion

        internal override Febraban.HeaderArquivo PreencheHeaderArquivo(Febraban.HeaderArquivo headerArquivo, List<Movimento> movimentos)
        {
            headerArquivo.Convenio =
                headerArquivo.CodigoBanco.ToString().PadLeft(4, '0') + //Nro Banco
                Empresa.NumeroAgencia.ToString().PadLeft(4, '0') + //Cod Agência s/ dígito verificador
                Empresa.Convenio.PadLeft(12, '0'); //N° Convênio

            return headerArquivo;
        }

        internal override HeaderLoteBase PreencheHeaderLote(HeaderLoteBase headerLote, TipoLancamentoEnum tipoLancamento)
        {
            ((HeaderLote)headerLote).Convenio =
                ((HeaderLote)headerLote).CodigoBanco.ToString().PadLeft(4, '0') + //Nro Banco
                Empresa.NumeroAgencia.ToString().PadLeft(4, '0') + //Cod Agência s/ dígito verificador
                Empresa.Convenio.PadLeft(12, '0'); //N° Convênio

            switch (tipoLancamento)
            {
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:
                case TipoLancamentoEnum.OrdemPagamento:
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.PIXTransferencia:
                    ((HeaderLote)headerLote).VersaoLote = 31; //Somente Segmento A  
                    break;
                case TipoLancamentoEnum.LiquidacaoProprioBanco:
                case TipoLancamentoEnum.PagamentoTituloOutroBanco:
                    ((HeaderLote)headerLote).VersaoLote = 30; //Demais Segmentos, nesse caso o J
                    break;
                case TipoLancamentoEnum.PagamentoTributosCodigoBarra:
                    ((HeaderLote)headerLote).VersaoLote = 10; //Segmento O 
                    break;
            }

            return headerLote;
        }

        internal override RegistroDetalheBase PreencheSegmentoA(RegistroDetalheBase segmento, Movimento movimento)
        {
            //if (segmento is SegmentoA_Transferencia ted)
            //{
            //    ted.CodigoFinalidadeComplementar = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta == TipoContaEnum.ContaCorrente ? "CC" : "PP";
            //}

            switch (movimento.TipoLancamento)
            {
                case TipoLancamentoEnum.TEDMesmaTitularidade:
                case TipoLancamentoEnum.TEDOutraTitularidade:
                case TipoLancamentoEnum.CreditoContaMesmoBanco:
                case TipoLancamentoEnum.CreditoContaPoupancaMesmoBanco:                
                    ((Febraban.SegmentoA)segmento).CodigoFinalidadeComplementar = ((MovimentoItemTransferenciaTED)movimento.MovimentoItem).TipoConta == TipoContaEnum.ContaCorrente ? "CC" : "PP";
                    break;
                //case TipoLancamentoEnum.PIXTransferencia:
                //case TipoLancamentoEnum.PIXQrCode:
                //    ((Febraban.SegmentoA)segmento).CodigoFinalidadeComplementar = "CC"; 
                //    break;
            }

            return segmento;
        }

        internal override RegistroDetalheBase PreencheSegmentoB(RegistroDetalheBase segmento, Movimento movimento)
        {
            if (segmento is SegmentoB_Transferencia ted)
            {
                ((SegmentoB_Transferencia)segmento).CodigoHistoricoParaCredito = 183;
            }

            if (segmento is SegmentoB_PIX pix)
            {
                if (pix.FormaIniciacao == FormaIniciacaoEnum.PIX_CPF_CNPJ) 
                {
                    pix.ChavePIX = pix.ChavePIX.JustNumbers();
                }
                else if (pix.FormaIniciacao == FormaIniciacaoEnum.PIX_Telefone) 
                {
                    if (!pix.ChavePIX.Contains("+55")) 
                    { 
                        pix.ChavePIX = "+55" + pix.ChavePIX.JustNumbers(); 
                    }
                    else
                    {
                        pix.ChavePIX = pix.ChavePIX.Substring(0, 3) + pix.ChavePIX.Substring(3).JustNumbers().PadRight(96, ' ');
                    }
                }
            }

            return segmento;
        }        
    }
}
